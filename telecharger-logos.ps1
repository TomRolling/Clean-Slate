# Télécharge le logo de chaque logiciel du catalogue et le range dans CleanSlate\Assets\Logos (64 x 64, PNG).
# Les adresses viennent de la base d'icônes du projet libre UniGetUI, faite pour les paquets winget.
# À relancer après avoir ajouté un logiciel. Un logo déjà présent n'est pas retéléchargé (-Tout pour tout refaire).
# Un logiciel sans logo garde simplement sa pastille de couleur avec son initiale.

param([switch]$Tout)

Add-Type -AssemblyName System.Drawing
$catalogue = Join-Path $PSScriptRoot 'CleanSlate\Services\Catalog.cs'
$dossier = Join-Path $PSScriptRoot 'CleanSlate\Assets\Logos'
New-Item -ItemType Directory -Force $dossier | Out-Null

# A(…), P(…) : logiciels winget ; S(…) : applis du Microsoft Store (leur identifiant ne dit rien : logo par la table $alias).
$ids = [regex]::Matches((Get-Content $catalogue -Raw), '\b[APS]\("[^"]*", "([^"]+)"') | ForEach-Object { $_.Groups[1].Value }
$base = (Invoke-WebRequest 'https://raw.githubusercontent.com/marticliment/UniGetUI/main/WebBasedData/screenshot-database-v2.json' -UseBasicParsing).Content |
    ConvertFrom-Json -AsHashtable
$icones = $base['icons_and_screenshots']

# Quand la base range le logo sous un autre nom que celui déduit de l'identifiant winget, ou n'en a pas :
# une clé de la base, ou directement l'adresse d'une image.
$tableau = 'https://cdn.jsdelivr.net/gh/homarr-labs/dashboard-icons/png'   # collection libre « dashboard-icons »
$favicon = 'https://www.google.com/s2/favicons?sz=128&domain='             # à défaut : l'icône du site de l'éditeur
$alias = @{
    'OpenAI.ChatGPT'                         = "$tableau/chatgpt.png"
    'EclipseAdoptium.Temurin.21.JRE'         = 'temurin-21-jdk'
    'Opera.Opera'                            = "$tableau/opera.png"
    'LibreWolf.LibreWolf'                    = "$tableau/librewolf.png"
    'TorProject.TorBrowser'                  = "$tableau/tor.png"
    'TeamSpeakSystems.TeamSpeakClient'       = "$tableau/teamspeak.png"
    'Mumble.Mumble.Client'                   = "$tableau/mumble.png"
    'Valve.Steam'                            = "$tableau/steam.png"
    'EpicGames.EpicGamesLauncher'            = "$tableau/epic-games.png"
    'RiotGames.LeagueOfLegends.EUW'          = "$tableau/league-of-legends.png"
    'RiotGames.Valorant.EU'                  = "$tableau/valorant.png"
    'Nvidia.GeForceNow'                      = "$tableau/nvidia.png"
    'Nvidia.PhysX'                           = "$tableau/nvidia.png"
    'Jellyfin.JellyfinMediaPlayer'           = "$tableau/jellyfin.png"
    'GIMP.GIMP.3'                            = "$tableau/gimp.png"
    'BlenderFoundation.Blender'              = "$tableau/blender.png"
    'Evernote.Evernote'                      = "$tableau/evernote.png"
    'KeePassXCTeam.KeePassXC'                = "$tableau/keepassxc.png"
    'Google.GoogleDrive'                     = "$tableau/google-drive.png"
    'Microsoft.OneDrive'                     = "$tableau/microsoft-onedrive.png"
    'Microsoft.VisualStudioCode'             = "$tableau/vscode.png"
    'GitHub.GitHubDesktop'                   = "$tableau/github.png"
    'GitHub.cli'                             = "$tableau/github.png"
    'WiresharkFoundation.Wireshark'          = "$tableau/wireshark.png"
    'Microsoft.DirectX'                      = "$tableau/microsoft.png"
    'Opera.OperaGX'                          = "${favicon}operagx.gg"
    'GOG.Galaxy'                             = "${favicon}gog.com"
    'CodecGuide.K-LiteCodecPack.Standard'    = "${favicon}codecguide.com"
    'OBSProject.OBSStudio'                   = "${favicon}obsproject.com"
    'Streamlabs.Streamlabs'                  = "${favicon}streamlabs.com"
    'Inkscape.Inkscape'                      = "${favicon}inkscape.org"
    'darktable.darktable'                    = "${favicon}darktable.org"
    'Malwarebytes.Malwarebytes'              = "${favicon}malwarebytes.com"
    'CrystalDewWorld.CrystalDiskInfo'        = "${favicon}crystalmark.info"
    'Corsair.iCUE.5'                         = "${favicon}corsair.com"
    'Brave.Brave'                            = "$tableau/brave.png"
    'Element.Element'                        = "$tableau/element.png"
    'Anthropic.Claude'                       = "$tableau/claude-ai.png"
    'Ollama.Ollama'                          = "$tableau/ollama.png"
    'Cloudflare.Warp'                        = "$tableau/cloudflare.png"
    'pCloudAG.pCloudDrive'                   = "$tableau/pcloud.png"
    'Moonsworth.LunarClient'                 = "${favicon}lunarclient.com"
    'Ankama.AnkamaLauncher'                  = "${favicon}ankama.com"
    'HeroicGamesLauncher.HeroicGamesLauncher' = "${favicon}heroicgameslauncher.com"
    'IrfanSkiljan.IrfanView'                 = "${favicon}irfanview.com"
    'MedalB.V.Medal'                         = "${favicon}medal.tv"
    'ElementLabs.LMStudio'                   = "${favicon}lmstudio.ai"
    'OO-Software.ShutUp10'                   = "${favicon}oo-software.com"
    'Anysphere.Cursor'                       = "${favicon}cursor.com"
    'Intel.IntelDriverAndSupportAssistant'   = "${favicon}intel.com"
    'RazerInc.RazerInstaller.Synapse4'       = "${favicon}razer.com"
    'CreativeTechnology.OpenAL'              = "${favicon}openal.org"
    # Applis du Microsoft Store
    '9NKSQGP7F2NH'                           = "$tableau/whatsapp.png"
    '9NBLGGH5L9XT'                           = "$tableau/instagram.png"
    '9NH2GPH4JZS4'                           = "$tableau/tiktok.png"
    '9MV0B5HZVK9Z'                           = "$tableau/xbox.png"
    '9NBLGGH30XJ3'                           = "$tableau/xbox.png"
    '9WZDNCRFJ3TJ'                           = "$tableau/netflix.png"
    '9P6RC76MSMMJ'                           = "$tableau/prime-video.png"
    '9NXQXXLFST89'                           = "$tableau/disney-plus.png"
    '9PFHDD62MXS1'                           = "$tableau/apple-music.png"
    '9NM4T8B9JQZ1'                           = "$tableau/apple-tv-plus.png"
    '9NBLGGH5R558'                           = "$tableau/microsoft-to-do.png"
    '9NHT9RB2F4HD'                           = "$tableau/microsoft-copilot.png"
    '9P9XG917PWCJ'                           = "$tableau/perplexity.png"
    # Intelligence artificielle
    'Google.GeminiDesktop'                   = "$tableau/google-gemini.png"
    'Anthropic.ClaudeCode'                   = "$tableau/claude-ai.png"
    'Comfy.ComfyUI-Desktop'                  = "$tableau/comfyui.png"
    'Jan.Jan'                                = "${favicon}jan.ai"
    'CloudStack.Msty'                        = "${favicon}msty.app"
    'pinokiocomputer.pinokio'                = "${favicon}pinokio.co"
    'Codeium.Windsurf'                       = "${favicon}windsurf.com"
    'Perplexity.Comet'                       = "${favicon}comet.perplexity.ai"
    '9PKTQ5699M62'                           = "$tableau/icloud.png"
    '9NP83LWLPZ9K'                           = "$tableau/apple.png"
    '9NF8H0H7WMLT'                           = "$tableau/nvidia.png"
}

function Get-Cles([string]$id) {
    $parts = $id.ToLower().Split('.')
    $reste = $parts[1..($parts.Count - 1)]
    @(($reste -join '-'), ($reste -join '.'), ($reste -join ''), $reste[0], ($id.ToLower() -replace '\.', '-')) | Select-Object -Unique
}

$manquants = @()
foreach ($id in $ids) {
    # Nom de fichier sans caractère spécial (le « + » de « 2015+ » gêne les ressources WPF).
    $fichier = Join-Path $dossier (($id -replace '[^A-Za-z0-9.-]', '_') + '.png')
    if ((Test-Path $fichier) -and -not $Tout) { continue }

    $url = $null
    if ($alias.ContainsKey($id)) {
        $a = $alias[$id]
        $url = if ($a -like 'http*') { $a } else { $icones[$a].icon }
    }
    else {
        foreach ($cle in Get-Cles $id) {
            if ($icones.ContainsKey($cle) -and $icones[$cle].icon) { $url = $icones[$cle].icon; break }
        }
    }
    if (-not $url) { $manquants += $id; continue }

    try {
        # Certains hébergeurs limitent le débit : trois essais, avec une pause qui s'allonge.
        $octets = $null
        foreach ($pause in 0, 4, 10) {
            Start-Sleep -Seconds $pause
            try { $octets = (Invoke-WebRequest $url -UseBasicParsing -TimeoutSec 25 -UserAgent 'CleanSlate-logos/1.0 (script de developpement)').Content; break }
            catch { }
        }
        if (-not $octets) { throw 'téléchargement impossible' }
        Start-Sleep -Milliseconds 700
        $source = [System.Drawing.Image]::FromStream([System.IO.MemoryStream]::new([byte[]]$octets))

        # Redimensionnement en gardant les proportions, centré sur un carré transparent.
        $taille = 64
        $echelle = [Math]::Min($taille / $source.Width, $taille / $source.Height)
        $l = [int]($source.Width * $echelle); $h = [int]($source.Height * $echelle)
        $image = New-Object System.Drawing.Bitmap $taille, $taille
        $g = [System.Drawing.Graphics]::FromImage($image)
        $g.InterpolationMode = 'HighQualityBicubic'; $g.PixelOffsetMode = 'HighQuality'; $g.SmoothingMode = 'HighQuality'
        $g.Clear([System.Drawing.Color]::Transparent)
        $g.DrawImage($source, [int](($taille - $l) / 2), [int](($taille - $h) / 2), $l, $h)
        $g.Dispose(); $source.Dispose()
        $image.Save($fichier, [System.Drawing.Imaging.ImageFormat]::Png)
        $image.Dispose()
        Write-Host "ok       $id"
    }
    catch {
        Write-Host "ECHEC    $id   $url" -ForegroundColor Yellow
        $manquants += $id
    }
}

Write-Host ""
Write-Host "$(@(Get-ChildItem $dossier -Filter *.png).Count) logos sur $($ids.Count) logiciels."
if ($manquants.Count -gt 0) { Write-Host "Sans logo : $($manquants -join ', ')" -ForegroundColor Yellow }
