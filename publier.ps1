# Fabrique le .exe autonome sur ce PC, pour l'essayer avant de l'envoyer.
#
# La publication, elle, est automatique : chaque « git push » sur la branche main lance les tests sur GitHub,
# fabrique le .exe et crée une Release (voir .github\workflows\publication.yml). Les applis déjà installées
# proposent alors la mise à jour à leur prochain lancement.

$ErrorActionPreference = 'Stop'
$projet = Join-Path $PSScriptRoot 'CleanSlate\CleanSlate.csproj'

dotnet test (Join-Path $PSScriptRoot 'CleanSlate.Tests\CleanSlate.Tests.csproj') -nologo -v q
if ($LASTEXITCODE -ne 0) { throw "Un test a échoué." }

dotnet publish $projet -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true -nologo -v q
if ($LASTEXITCODE -ne 0) { throw "La compilation a échoué." }

$exe = Join-Path $PSScriptRoot 'CleanSlatein\Release
et10.0-windows\win-x64\publish\CleanSlate.exe'
Write-Host "Fichier prêt : $exe ($([Math]::Round((Get-Item $exe).Length / 1MB)) Mo)"
