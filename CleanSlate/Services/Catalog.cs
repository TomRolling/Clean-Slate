using CleanSlate.Models;

namespace CleanSlate.Services;

/// <summary>Une famille de logiciels (une entrée de la barre latérale).</summary>
/// <param name="Icon">Caractère de la police d'icônes Windows.</param>
public sealed record CatalogCategory(string Name, string Icon, IReadOnlyList<AppItem> Apps);

/// <summary>Un pack : un nom, une icône, une phrase, et les identifiants des logiciels qu'il coche.</summary>
public sealed record PackDefinition(string Name, string Icon, string Description, IReadOnlyList<string> Ids);

/// <summary>
/// La liste de tout ce que l'appli sait installer. Pour ajouter un logiciel : une ligne ici, rien d'autre.
/// Chaque identifiant winget a été vérifié avec « winget show --id … --exact --source winget »
/// (le script verifier-catalogue.ps1 les revérifie tous d'un coup).
/// A(…) = logiciel normal, P(…) = logiciel « populaire » (affiché aussi dans la première page),
/// S(…) = appli du Microsoft Store (identifiant du Store, puis nom de son paquet Windows).
/// Ce qui est payant ou demande un compte est noté à part, dans CatalogAccess.cs.
/// </summary>
public static class Catalog
{
    /// <summary>Nom de la famille installée en premier : les logiciels ont besoin de ces composants pour démarrer.</summary>
    public const string ComponentsCategory = "Composants système";

    public static IReadOnlyList<CatalogCategory> Create() =>
    [
        Category("Navigateurs", "",
            P("Google Chrome", "Google.Chrome", "Le navigateur le plus utilisé"),
            P("Mozilla Firefox", "Mozilla.Firefox", "Libre et respectueux de la vie privée"),
            P("Brave", "Brave.Brave", "Bloque les pubs et les traqueurs d'origine"),
            A("Opera", "Opera.Opera", "Avec VPN et messageries intégrés"),
            P("Opera GX", "Opera.OperaGX", "Le navigateur pensé pour les joueurs"),
            A("Microsoft Edge", "Microsoft.Edge", "Le navigateur de Windows"),
            A("Vivaldi", "Vivaldi.Vivaldi", "Ultra personnalisable"),
            A("Zen Browser", "Zen-Team.Zen-Browser", "Firefox repensé, épuré et moderne"),
            A("LibreWolf", "LibreWolf.LibreWolf", "Firefox réglé pour la confidentialité"),
            A("Waterfox", "Waterfox.Waterfox", "Dérivé de Firefox, sans télémétrie"),
            A("DuckDuckGo", "DuckDuckGo.DesktopBrowser", "Navigation privée par défaut"),
            A("Tor Browser", "TorProject.TorBrowser", "Navigation anonyme par le réseau Tor"),
            A("Arc", "TheBrowserCompany.Arc", "Navigateur au design différent")),

        Category("Communication", "",
            P("Discord", "Discord.Discord", "Vocal et discussion pour les communautés"),
            A("Telegram", "Telegram.TelegramDesktop", "Messagerie rapide et synchronisée"),
            A("Signal", "OpenWhisperSystems.Signal", "Messagerie chiffrée"),
            A("Zoom", "Zoom.Zoom", "Visioconférence"),
            A("Microsoft Teams", "Microsoft.Teams", "Réunions et travail en équipe"),
            A("Slack", "SlackTechnologies.Slack", "Messagerie d'équipe"),
            A("TeamSpeak", "TeamSpeakSystems.TeamSpeakClient", "Vocal léger pour jouer"),
            A("Mumble", "Mumble.Mumble.Client", "Vocal libre à faible latence"),
            A("Element", "Element.Element", "Messagerie décentralisée (Matrix)"),
            A("Thunderbird", "Mozilla.Thunderbird", "Logiciel de courriel libre"),
            A("Proton Mail", "Proton.ProtonMail", "Courriel chiffré"),
            S("WhatsApp", "9NKSQGP7F2NH", "5319275A.WhatsAppDesktop_cv1g1gvanyjgm", "Messagerie reliée à ton téléphone"),
            S("Instagram", "9NBLGGH5L9XT", "Facebook.InstagramBeta_8xx8rvfyw5nnt", "Photos, stories et messages"),
            S("TikTok", "9NH2GPH4JZS4", "BytedancePte.Ltd.TikTok_6yccndn6064se", "Vidéos courtes")),

        Category("Jeux", "",
            P("Steam", "Valve.Steam", "La plus grande boutique de jeux PC"),
            P("Epic Games Launcher", "EpicGames.EpicGamesLauncher", "Fortnite, Rocket League, jeux offerts"),
            A("GOG Galaxy", "GOG.Galaxy", "Jeux sans DRM, toutes tes bibliothèques réunies"),
            A("Ubisoft Connect", "Ubisoft.Connect", "Les jeux Ubisoft"),
            A("EA app", "ElectronicArts.EADesktop", "Les jeux Electronic Arts"),
            A("Rockstar Games Launcher", "RockstarGames.Launcher", "GTA, Red Dead Redemption"),
            A("Amazon Games", "Amazon.Games", "Jeux offerts avec Prime Gaming"),
            A("League of Legends", "RiotGames.LeagueOfLegends.EUW", "Serveur Europe de l'Ouest"),
            A("Valorant", "RiotGames.Valorant.EU", "Serveur Europe"),
            A("Minecraft Launcher", "Mojang.MinecraftLauncher", "Le lanceur officiel de Minecraft"),
            A("Prism Launcher", "PrismLauncher.PrismLauncher", "Lanceur Minecraft libre, multi-instances"),
            A("Modrinth App", "Modrinth.ModrinthApp", "Mods et modpacks Minecraft"),
            A("CurseForge", "Overwolf.CurseForge", "Mods pour Minecraft, WoW et d'autres"),
            A("Lunar Client", "Moonsworth.LunarClient", "Client Minecraft optimisé"),
            A("Ankama Launcher", "Ankama.AnkamaLauncher", "Dofus, Wakfu"),
            A("itch.io", "ItchIo.Itch", "Jeux indépendants"),
            A("Heroic Games Launcher", "HeroicGamesLauncher.HeroicGamesLauncher", "Epic, GOG et Amazon dans un lanceur libre"),
            A("Playnite", "Playnite.Playnite", "Tous tes jeux dans une seule bibliothèque"),
            A("GeForce NOW", "Nvidia.GeForceNow", "Jouer en streaming depuis le cloud"),
            A("Parsec", "Parsec.Parsec", "Jouer à distance, faible latence"),
            A("Moonlight", "MoonlightGameStreamingProject.Moonlight", "Streamer ses jeux depuis un autre PC"),
            A("Sunshine", "LizardByte.Sunshine", "Serveur de streaming de jeux pour Moonlight"),
            A("WeMod", "WeMod.WeMod", "Triches et mods pour jeux solo"),
            A("Vortex", "NexusMods.Vortex", "Gestionnaire de mods Nexus"),
            S("Xbox", "9MV0B5HZVK9Z", "Microsoft.GamingApp_8wekyb3d8bbwe", "Game Pass et jeux Microsoft"),
            S("Accessoires Xbox", "9NBLGGH30XJ3", "Microsoft.XboxDevices_8wekyb3d8bbwe", "Régler et mettre à jour sa manette Xbox")),

        Category("Multimédia", "",
            P("VLC", "VideoLAN.VLC", "Lit toutes les vidéos et musiques"),
            P("Spotify", "Spotify.Spotify", "Musique et podcasts en streaming"),
            A("Deezer", "Deezer.Deezer", "Musique en streaming"),
            A("iTunes", "Apple.iTunes", "Musique et synchronisation iPhone"),
            A("MPC-HC", "clsid2.mpc-hc", "Lecteur vidéo très léger"),
            A("mpv", "mpv.net", "Lecteur vidéo minimaliste"),
            A("K-Lite Codec Pack", "CodecGuide.K-LiteCodecPack.Standard", "Tous les codecs audio et vidéo"),
            A("AIMP", "AIMP.AIMP", "Lecteur de musique"),
            A("foobar2000", "PeterPawlowski.foobar2000", "Lecteur de musique léger et modulable"),
            A("Audacity", "Audacity.Audacity", "Enregistrer et retoucher du son"),
            A("HandBrake", "HandBrake.HandBrake", "Convertir des vidéos"),
            A("Plex", "Plex.Plex", "Regarder sa médiathèque Plex"),
            A("Jellyfin Media Player", "Jellyfin.JellyfinMediaPlayer", "Regarder sa médiathèque Jellyfin"),
            A("Kodi", "XBMCFoundation.Kodi", "Centre multimédia pour le salon"),
            A("Stremio", "Stremio.Stremio", "Films et séries regroupés"),
            S("Netflix", "9WZDNCRFJ3TJ", "4DF9E0F8.Netflix_mcm4njqhnhss8", "Films et séries en streaming"),
            S("Prime Video", "9P6RC76MSMMJ", "AmazonVideo.PrimeVideo_pwbj9vvecjh7j", "Films et séries d'Amazon"),
            S("Disney+", "9NXQXXLFST89", "Disney.37853FC22B2CE_6rarf9sa4v8jt", "Disney, Marvel, Star Wars"),
            S("Apple Music", "9PFHDD62MXS1", "AppleInc.AppleMusicWin_nzyj5cx40ttqa", "Musique en streaming d'Apple"),
            S("Apple TV", "9NM4T8B9JQZ1", "AppleInc.AppleTVWin_nzyj5cx40ttqa", "Films, séries et Apple TV+")),

        Category("Création", "",
            P("OBS Studio", "OBSProject.OBSStudio", "Enregistrer son écran et streamer"),
            A("Streamlabs Desktop", "Streamlabs.Streamlabs", "Streaming avec alertes et thèmes"),
            A("GIMP", "GIMP.GIMP.3", "Retouche photo libre"),
            A("Paint.NET", "dotPDN.PaintDotNet", "Retouche d'image simple"),
            A("Krita", "KDE.Krita", "Dessin et peinture numérique"),
            A("Inkscape", "Inkscape.Inkscape", "Dessin vectoriel"),
            A("Blender", "BlenderFoundation.Blender", "Modélisation et animation 3D"),
            A("Kdenlive", "KDE.Kdenlive", "Montage vidéo libre"),
            A("Shotcut", "Meltytech.Shotcut", "Montage vidéo simple"),
            A("CapCut", "ByteDance.CapCut", "Montage vidéo pour les réseaux"),
            A("Figma", "Figma.Figma", "Maquettes d'interfaces"),
            A("Canva", "Canva.Canva", "Visuels et affiches faciles"),
            A("darktable", "darktable.darktable", "Développement de photos RAW"),
            A("IrfanView", "IrfanSkiljan.IrfanView", "Visionneuse d'images ultra rapide"),
            A("ShareX", "ShareX.ShareX", "Captures d'écran et GIF"),
            A("Greenshot", "Greenshot.Greenshot", "Captures d'écran annotées"),
            A("Medal", "MedalB.V.Medal", "Enregistrer ses clips de jeu")),

        Category("Bureautique", "",
            P("LibreOffice", "TheDocumentFoundation.LibreOffice", "Traitement de texte, tableur : gratuit"),
            A("OnlyOffice", "ONLYOFFICE.DesktopEditors", "Suite bureautique proche de Microsoft Office"),
            P("Adobe Acrobat Reader", "Adobe.Acrobat.Reader.64-bit", "Lire et signer les PDF"),
            A("SumatraPDF", "SumatraPDF.SumatraPDF", "Lecteur PDF minuscule et rapide"),
            A("Foxit PDF Reader", "Foxit.FoxitReader", "Lecteur PDF complet"),
            A("PDFsam Basic", "PDFsam.PDFsam", "Fusionner et découper des PDF"),
            P("Notepad++", "Notepad++.Notepad++", "Éditeur de texte"),
            A("Notion", "Notion.Notion", "Notes, tâches et documents"),
            A("Obsidian", "Obsidian.Obsidian", "Prise de notes reliées entre elles"),
            A("Evernote", "Evernote.Evernote", "Prise de notes"),
            A("calibre", "calibre.calibre", "Gérer ses livres numériques"),
            S("Microsoft To Do", "9NBLGGH5R558", "Microsoft.Todos_8wekyb3d8bbwe", "Listes de tâches synchronisées")),

        Category("Intelligence artificielle", "",
            A("ChatGPT", "OpenAI.ChatGPT", "L'assistant IA d'OpenAI"),
            A("Claude", "Anthropic.Claude", "L'assistant IA d'Anthropic"),
            A("Gemini", "Google.GeminiDesktop", "L'assistant IA de Google"),
            S("Microsoft Copilot", "9NHT9RB2F4HD", "Microsoft.Copilot_8wekyb3d8bbwe", "L'assistant IA de Microsoft"),
            S("Perplexity", "9P9XG917PWCJ", "PerplexityAI.PerplexityApp_3jh4kjrg4dzr2", "Recherche sur le web avec réponses sourcées"),
            A("Comet", "Perplexity.Comet", "Le navigateur à IA de Perplexity"),
            A("DeepL", "DeepL.DeepL", "Traduction de très bonne qualité"),
            A("Ollama", "Ollama.Ollama", "Faire tourner des IA sur son PC"),
            A("LM Studio", "ElementLabs.LMStudio", "IA locales avec une interface"),
            A("Jan", "Jan.Jan", "IA locales, libre et hors ligne"),
            A("GPT4All", "nomic.gpt4all", "IA locales, simple à prendre en main"),
            A("Msty", "CloudStack.Msty", "IA locales et en ligne dans une même appli"),
            A("ComfyUI", "Comfy.ComfyUI-Desktop", "Créer des images par IA sur son PC"),
            A("Upscayl", "Upscayl.Upscayl", "Agrandir une image sans la flouter"),
            A("Pinokio", "pinokiocomputer.pinokio", "Installer des outils d'IA en un clic"),
            A("Claude Code", "Anthropic.ClaudeCode", "L'assistant de code d'Anthropic, dans le terminal")),

        Category("Utilitaires", "",
            P("7-Zip", "7zip.7zip", "Ouvrir et créer des archives (zip, rar, 7z)"),
            A("WinRAR", "RARLab.WinRAR", "Archives RAR et ZIP"),
            A("NanaZip", "M2Team.NanaZip", "7-Zip au goût de Windows 11"),
            A("PowerToys", "Microsoft.PowerToys", "Outils Microsoft pour utilisateurs avancés"),
            A("Everything", "voidtools.Everything", "Trouver un fichier instantanément"),
            A("WizTree", "AntibodySoftware.WizTree", "Voir ce qui remplit le disque"),
            A("WinDirStat", "WinDirStat.WinDirStat", "Carte de l'espace disque"),
            A("TreeSize Free", "JAMSoftware.TreeSize.Free", "Taille des dossiers"),
            A("qBittorrent", "qBittorrent.qBittorrent", "Client torrent sans pub"),
            A("Rufus", "Rufus.Rufus", "Créer une clé USB d'installation"),
            A("Ventoy", "Ventoy.Ventoy", "Clé USB avec plusieurs systèmes"),
            A("balenaEtcher", "Balena.Etcher", "Graver une image sur clé ou carte SD"),
            A("TeamViewer", "TeamViewer.TeamViewer", "Prise en main à distance"),
            A("AnyDesk", "AnyDesk.AnyDesk", "Prise en main à distance"),
            A("Bulk Crap Uninstaller", "Klocman.BulkCrapUninstaller", "Désinstaller en masse"),
            A("Revo Uninstaller", "RevoUninstaller.RevoUninstaller", "Désinstaller sans laisser de traces"),
            A("EarTrumpet", "File-New-Project.EarTrumpet", "Volume par application"),
            A("Ditto", "Ditto.Ditto", "Historique du presse-papiers"),
            A("Flow Launcher", "Flow-Launcher.Flow-Launcher", "Lancer tout au clavier"),
            A("AutoHotkey", "AutoHotkey.AutoHotkey", "Raccourcis et automatisations"),
            A("LocalSend", "LocalSend.LocalSend", "Envoyer des fichiers entre appareils"),
            A("KDE Connect", "KDE.KDEConnect", "Relier son téléphone au PC"),
            A("O&O ShutUp10++", "OO-Software.ShutUp10", "Régler la confidentialité de Windows")),

        Category("Sécurité et VPN", "",
            P("Bitwarden", "Bitwarden.Bitwarden", "Gestionnaire de mots de passe"),
            A("KeePassXC", "KeePassXCTeam.KeePassXC", "Mots de passe gardés sur le PC"),
            A("1Password", "AgileBits.1Password", "Gestionnaire de mots de passe"),
            A("Proton Pass", "Proton.ProtonPass", "Gestionnaire de mots de passe chiffré"),
            A("Malwarebytes", "Malwarebytes.Malwarebytes", "Chasse les logiciels malveillants"),
            A("Proton VPN", "Proton.ProtonVPN", "VPN avec offre gratuite"),
            A("NordVPN", "NordSecurity.NordVPN", "VPN (abonnement)"),
            A("Mullvad VPN", "MullvadVPN.MullvadVPN", "VPN axé sur l'anonymat"),
            A("Cloudflare WARP", "Cloudflare.Warp", "Connexion chiffrée gratuite"),
            A("WireGuard", "WireGuard.WireGuard", "Client VPN moderne"),
            A("OpenVPN Connect", "OpenVPNTechnologies.OpenVPNConnect", "Client OpenVPN officiel"),
            A("Tailscale", "Tailscale.Tailscale", "Réseau privé entre ses appareils"),
            A("VeraCrypt", "IDRIX.VeraCrypt", "Chiffrer un disque ou un dossier")),

        Category("Cloud et fichiers", "",
            A("Google Drive", "Google.GoogleDrive", "Synchroniser Google Drive"),
            A("Dropbox", "Dropbox.Dropbox", "Synchroniser Dropbox"),
            A("OneDrive", "Microsoft.OneDrive", "Le stockage en ligne de Microsoft"),
            A("MEGA", "Mega.MEGASync", "Stockage en ligne chiffré"),
            A("Proton Drive", "Proton.ProtonDrive", "Stockage en ligne chiffré"),
            A("pCloud Drive", "pCloudAG.pCloudDrive", "Stockage en ligne"),
            A("Nextcloud", "Nextcloud.NextcloudDesktop", "Synchroniser son propre cloud"),
            A("Syncthing Tray", "Martchus.syncthingtray", "Synchroniser entre appareils, sans serveur"),
            A("WinSCP", "WinSCP.WinSCP", "Transfert de fichiers SFTP"),
            S("iCloud", "9PKTQ5699M62", "AppleInc.iCloud_nzyj5cx40ttqa", "Photos et fichiers iCloud sur le PC"),
            S("Appareils Apple", "9NP83LWLPZ9K", "AppleInc.AppleDevices_nzyj5cx40ttqa", "Sauvegarder et synchroniser un iPhone")),

        Category("Développement", "",
            P("Visual Studio Code", "Microsoft.VisualStudioCode", "L'éditeur de code le plus utilisé"),
            A("Cursor", "Anysphere.Cursor", "Éditeur de code avec IA"),
            A("Windsurf", "Codeium.Windsurf", "Éditeur de code avec IA"),
            A("Visual Studio 2022 Community", "Microsoft.VisualStudio.2022.Community", "L'environnement complet de Microsoft"),
            A("JetBrains Toolbox", "JetBrains.Toolbox", "Installer IntelliJ, PyCharm, Rider…"),
            P("Git", "Git.Git", "Gestion de versions"),
            A("GitHub Desktop", "GitHub.GitHubDesktop", "Git avec une interface simple"),
            A("GitHub CLI", "GitHub.cli", "GitHub en ligne de commande"),
            A("Windows Terminal", "Microsoft.WindowsTerminal", "Le terminal moderne de Windows"),
            A("PowerShell 7", "Microsoft.PowerShell", "La version récente de PowerShell"),
            A("Python 3.13", "Python.Python.3.13", "Le langage Python"),
            A("Node.js LTS", "OpenJS.NodeJS.LTS", "JavaScript côté serveur"),
            A("Java JDK 21 (Temurin)", "EclipseAdoptium.Temurin.21.JDK", "Pour développer en Java"),
            A("Docker Desktop", "Docker.DockerDesktop", "Conteneurs"),
            A("VirtualBox", "Oracle.VirtualBox", "Machines virtuelles"),
            A("Postman", "Postman.Postman", "Tester des API"),
            A("DBeaver", "DBeaver.DBeaver.Community", "Explorer des bases de données"),
            A("PuTTY", "PuTTY.PuTTY", "Client SSH"),
            A("Wireshark", "WiresharkFoundation.Wireshark", "Analyser le trafic réseau"),
            A("Arduino IDE", "ArduinoSA.IDE.stable", "Programmer des cartes Arduino")),

        Category("Pilotes et matériel", "",
            A("Intel Driver & Support Assistant", "Intel.IntelDriverAndSupportAssistant", "Pilotes Intel à jour"),
            A("MSI Afterburner", "Guru3D.Afterburner", "Surveiller et régler sa carte graphique"),
            A("HWiNFO", "REALiX.HWiNFO", "Températures et capteurs en détail"),
            A("HWMonitor", "CPUID.HWMonitor", "Températures et tensions"),
            A("CPU-Z", "CPUID.CPU-Z", "Tout savoir sur son processeur"),
            A("GPU-Z", "TechPowerUp.GPU-Z", "Tout savoir sur sa carte graphique"),
            A("CrystalDiskInfo", "CrystalDewWorld.CrystalDiskInfo", "Santé des disques"),
            A("CrystalDiskMark", "CrystalDewWorld.CrystalDiskMark", "Vitesse des disques"),
            A("Logitech G HUB", "Logitech.GHUB", "Souris, claviers et casques Logitech G"),
            A("Razer Synapse", "RazerInc.RazerInstaller.Synapse4", "Périphériques Razer"),
            A("Corsair iCUE", "Corsair.iCUE.5", "Périphériques Corsair"),
            A("SteelSeries GG", "SteelSeries.GG", "Périphériques SteelSeries"),
            A("Elgato Stream Deck", "Elgato.StreamDeck", "Le logiciel du Stream Deck"),
            A("OpenRGB", "OpenRGB.OpenRGB", "Tout l'éclairage RGB dans une appli libre"),
            S("Panneau NVIDIA", "9NF8H0H7WMLT", "NVIDIACorp.NVIDIAControlPanel_56jybvy8sckqj", "Réglages de la carte graphique NVIDIA")),

        Category(ComponentsCategory, "",
            P("DirectX", "Microsoft.DirectX", "Les anciennes DLL demandées par beaucoup de jeux"),
            P("VC++ 2015-2022 x64", "Microsoft.VCRedist.2015+.x64", "Visual C++ : demandé par presque tous les jeux"),
            P("VC++ 2015-2022 x86", "Microsoft.VCRedist.2015+.x86", "Visual C++ 32 bits, tout aussi demandé"),
            A("VC++ 2013 x64", "Microsoft.VCRedist.2013.x64", "Pour les logiciels de cette époque"),
            A("VC++ 2013 x86", "Microsoft.VCRedist.2013.x86", "Pour les logiciels de cette époque"),
            A("VC++ 2012 x64", "Microsoft.VCRedist.2012.x64", "Pour les logiciels de cette époque"),
            A("VC++ 2012 x86", "Microsoft.VCRedist.2012.x86", "Pour les logiciels de cette époque"),
            A("VC++ 2010 x64", "Microsoft.VCRedist.2010.x64", "Pour les anciens jeux"),
            A("VC++ 2010 x86", "Microsoft.VCRedist.2010.x86", "Pour les anciens jeux"),
            A("VC++ 2008 x64", "Microsoft.VCRedist.2008.x64", "Pour les anciens jeux"),
            A("VC++ 2008 x86", "Microsoft.VCRedist.2008.x86", "Pour les anciens jeux"),
            A("VC++ 2005 x64", "Microsoft.VCRedist.2005.x64", "Pour les très anciens jeux"),
            A("VC++ 2005 x86", "Microsoft.VCRedist.2005.x86", "Pour les très anciens jeux"),
            A(".NET Desktop 10", "Microsoft.DotNet.DesktopRuntime.10", "Pour les logiciels .NET récents"),
            A(".NET Desktop 9", "Microsoft.DotNet.DesktopRuntime.9", "Pour les logiciels .NET récents"),
            P(".NET Desktop 8", "Microsoft.DotNet.DesktopRuntime.8", "Le .NET Runtime le plus demandé"),
            A(".NET Desktop 6", "Microsoft.DotNet.DesktopRuntime.6", "Pour les logiciels .NET un peu plus anciens"),
            A(".NET Framework 4.8.1", "Microsoft.DotNet.Framework.Runtime", "Pour les logiciels Windows classiques"),
            A("WebView2 Runtime", "Microsoft.EdgeWebView2Runtime", "Affichage web utilisé par beaucoup d'applis"),
            A("Java 8 (Oracle)", "Oracle.JavaRuntimeEnvironment", "Pour les logiciels et jeux en Java"),
            A("Java 21 (Temurin)", "EclipseAdoptium.Temurin.21.JRE", "Java récent, pour Minecraft notamment"),
            A("XNA Framework 4.0", "Microsoft.XNARedist", "Pour certains jeux (Terraria, Stardew Valley)"),
            A("OpenAL", "CreativeTechnology.OpenAL", "Son 3D de certains jeux"),
            A("NVIDIA PhysX", "Nvidia.PhysX", "Moteur physique de certains jeux")),
    ];

    private static CatalogCategory Category(string name, string icon, params AppItem[] apps)
    {
        foreach (var app in apps)
        {
            app.Category = name;
            app.IndexCategory();
            app.LogoUri = Logos.Find(app.WingetId);
            app.Access = CatalogAccess.Of(app.WingetId);
            // Les composants système vont toujours sur le disque de Windows : on ne propose pas de choisir.
            // Les applis du Microsoft Store non plus : c'est Windows qui les range.
            app.CanChooseLocation = name != ComponentsCategory
                && !app.IsStoreApp
                && app.WingetId != "EclipseAdoptium.Temurin.21.JDK"
                && CatalogLocations.Choosable.Contains(app.WingetId);
        }
        return new CatalogCategory(name, icon, apps);
    }

    private static AppItem A(string name, string wingetId, string description) => new(name, wingetId, description, isPopular: false);
    private static AppItem P(string name, string wingetId, string description) => new(name, wingetId, description, isPopular: true);
    private static AppItem S(string name, string storeId, string storePackage, string description) =>
        new(name, storeId, description, isPopular: false, storePackage);

    /// <summary>
    /// Les packs : des sélections toutes faites pour un usage. Un clic coche tout le lot, on ajuste ensuite.
    /// Aucun navigateur dedans : c'est un choix trop personnel.
    /// </summary>
    public static IReadOnlyList<PackDefinition> Packs { get; } =
    [
        new("Gamer", "\uE7FC", "Les boutiques de jeux, Discord et les composants que les jeux réclament",
        [
            "Valve.Steam", "EpicGames.EpicGamesLauncher", "Discord.Discord", "Guru3D.Afterburner", "7zip.7zip",
            "Microsoft.DirectX", "Microsoft.VCRedist.2015+.x64", "Microsoft.VCRedist.2015+.x86",
            "Microsoft.DotNet.DesktopRuntime.8", "Microsoft.XNARedist",
        ]),
        new("Bureautique", "\uE8A5", "Écrire, lire des PDF, gérer ses courriels et ses mots de passe",
        [
            "TheDocumentFoundation.LibreOffice", "Adobe.Acrobat.Reader.64-bit", "Mozilla.Thunderbird", "7zip.7zip",
            "Notepad++.Notepad++", "VideoLAN.VLC", "Bitwarden.Bitwarden",
        ]),
        new("Étudiant", "\uE7BE", "Cours, travail en groupe, prise de notes et un peu de détente",
        [
            "TheDocumentFoundation.LibreOffice", "Adobe.Acrobat.Reader.64-bit", "Obsidian.Obsidian", "Zoom.Zoom",
            "Microsoft.Teams", "Discord.Discord", "7zip.7zip", "VideoLAN.VLC", "Spotify.Spotify", "Bitwarden.Bitwarden",
        ]),
        new("Streaming", "\uE714", "Diffuser en direct, enregistrer, monter ses vidéos",
        [
            "OBSProject.OBSStudio", "Discord.Discord", "ShareX.ShareX", "Audacity.Audacity", "KDE.Kdenlive",
            "HandBrake.HandBrake", "VideoLAN.VLC", "Microsoft.VCRedist.2015+.x64",
        ]),
        new("Développeur", "\uE943", "Éditeur de code, Git, terminal et langages courants",
        [
            "Microsoft.VisualStudioCode", "Git.Git", "GitHub.GitHubDesktop", "Microsoft.WindowsTerminal",
            "Microsoft.PowerShell", "Python.Python.3.13", "OpenJS.NodeJS.LTS", "Notepad++.Notepad++", "7zip.7zip",
        ]),
    ];
}
