# À relancer après avoir ajouté un logiciel dans CleanSlate\Services\Catalog.cs. Compter quelques minutes.
#   1. Vérifie que chaque identifiant winget du catalogue existe vraiment (y compris les applis du Microsoft Store,
#      et que le nom de paquet Windows noté pour elles est bien celui que donne le Store).
#   2. Lit la fiche d'installation de chaque logiciel (dépôt public microsoft/winget-pkgs) pour savoir s'il accepte
#      qu'on choisisse son dossier d'installation, et réécrit CleanSlate\Services\CatalogLocations.cs.

$fichier = Join-Path $PSScriptRoot 'CleanSlate\Services\Catalog.cs'
$sortieCs = Join-Path $PSScriptRoot 'CleanSlate\Services\CatalogLocations.cs'
$ids = [regex]::Matches((Get-Content $fichier -Raw), '\b[AP]\("[^"]*", "([^"]+)"') | ForEach-Object { $_.Groups[1].Value }
Write-Host "$($ids.Count) identifiants à vérifier..."

$doublons = $ids | Group-Object | Where-Object Count -gt 1
foreach ($d in $doublons) { Write-Host "EN DOUBLE  $($d.Name)" -ForegroundColor Yellow }

$absents = @(); $choix = @(); $inconnus = @()

# Applis du Microsoft Store : S("Nom", "identifiant du Store", "nom du paquet Windows", …)
$store = [regex]::Matches((Get-Content $fichier -Raw), '\bS\("[^"]*", "([^"]+)", "([^"]+)"')
Write-Host "$($store.Count) applis du Microsoft Store à vérifier..."
foreach ($m in $store) {
    $id = $m.Groups[1].Value; $paquet = $m.Groups[2].Value
    winget show --id $id --exact --source msstore --accept-source-agreements --disable-interactivity 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Host "ABSENT   $id (Store)" -ForegroundColor Red; $absents += $id; continue }
    try {
        # La fiche publique du Store (celle que lit winget) donne le nom du paquet Windows de l'appli.
        $fiche = Invoke-RestMethod "https://storeedgefd.dsx.mp.microsoft.com/v9.0/packageManifests/$id" -TimeoutSec 20
        $noms = @($fiche.Data.Versions[0].Installers | ForEach-Object PackageFamilyName | Select-Object -Unique)
        if ($noms -contains $paquet) { Write-Host ("ok       {0,-45} Store" -f $id) }
        else { Write-Host "PAQUET   $id : le catalogue dit $paquet, le Store dit $($noms -join ', ')" -ForegroundColor Red; $absents += $id }
    }
    catch { Write-Host ("ok       {0,-45} Store (nom de paquet non revérifié : fiche injoignable)" -f $id) -ForegroundColor Yellow }
}

foreach ($id in $ids) {
    $sortie = winget show --id $id --exact --source winget --accept-source-agreements --disable-interactivity 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ABSENT   $id" -ForegroundColor Red
        $absents += $id
        continue
    }

    # Fiche d'installation : manifests/<1re lettre>/<identifiant découpé aux points>/<version>/<identifiant>.installer.yaml
    $version = [regex]::Match($sortie, '(?m)^Version\s*:\s*(\S+)').Groups[1].Value
    $chemin = "$($id.Substring(0,1).ToLower())/$($id -replace '\.', '/')/$version/$id.installer.yaml" -replace '\+', '%2B'
    $etat = '?'
    try {
        $yaml = (Invoke-WebRequest "https://raw.githubusercontent.com/microsoft/winget-pkgs/master/manifests/$chemin" -UseBasicParsing).Content
        # Le dossier est respecté si la fiche donne l'option de l'installeur (InstallLocation), ou si l'installeur est
        # d'un type dont l'option est connue de winget (Inno Setup, Nullsoft) ou une simple archive « portable ».
        $ok = $yaml -match '(?m)^\s*InstallLocation\s*:' -or $yaml -match '(?mi)^\s*-?\s*(Nested)?InstallerType\s*:\s*(inno|nullsoft|portable)\s*$'
        $etat = if ($ok) { 'dossier au choix' } else { 'dossier imposé' }
        if ($ok) { $choix += $id }
    }
    catch { $inconnus += $id }
    Write-Host ("ok       {0,-45} {1}" -f $id, $etat)
}

$lignes = @(
    'namespace CleanSlate.Services;'
    ''
    '/// <summary>'
    '/// Fichier écrit par verifier-catalogue.ps1 : ne pas le modifier à la main.'
    '/// Les logiciels dont l''installeur accepte qu''on choisisse le dossier d''installation (option « --location » de winget).'
    '/// Pour les autres, l''éditeur impose l''emplacement : l''appli ne propose pas le choix.'
    '/// </summary>'
    'public static class CatalogLocations'
    '{'
    '    public static readonly HashSet<string> Choosable = new(StringComparer.OrdinalIgnoreCase)'
    '    {'
) + ($choix | ForEach-Object { "        `"$_`"," }) + @('    };', '}')
[IO.File]::WriteAllLines($sortieCs, $lignes, [Text.UTF8Encoding]::new($false))

Write-Host ""
Write-Host "$($choix.Count) logiciels acceptent un dossier au choix (liste écrite dans CatalogLocations.cs)."
if ($inconnus.Count -gt 0) { Write-Host "Fiche introuvable (dossier considéré comme imposé) : $($inconnus -join ', ')" -ForegroundColor Yellow }
if ($absents.Count -eq 0 -and -not $doublons) { Write-Host "Tout le catalogue est bon." -ForegroundColor Green }
else { Write-Host "$($absents.Count) identifiant(s) introuvable(s) : $($absents -join ', ')" -ForegroundColor Red; exit 1 }
