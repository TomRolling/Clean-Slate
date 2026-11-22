using CleanSlate.Models;

namespace CleanSlate.Services;

/// <summary>
/// Ce qui n'est pas simplement gratuit : les logiciels qui demandent un compte, et ceux qui sont payants.
/// Liste tenue à la main (winget ne donne pas cette information) ; tout logiciel absent d'ici est affiché « Gratuit ».
/// </summary>
public static class CatalogAccess
{
    public static Access Of(string id) =>
        Paid.Contains(id) ? Access.Paid
        : Trial.Contains(id) ? Access.Trial
        : Account.Contains(id) ? Access.Account
        : Access.Free;

    /// <summary>Abonnement ou achat nécessaire pour s'en servir.</summary>
    public static readonly HashSet<string> Paid = new(StringComparer.OrdinalIgnoreCase)
    {
        "AgileBits.1Password", "NordSecurity.NordVPN", "MullvadVPN.MullvadVPN",
        "Mojang.MinecraftLauncher",   // le lanceur est gratuit, le jeu ne l'est pas
        "9WZDNCRFJ3TJ",               // Netflix
        "9P6RC76MSMMJ",               // Prime Video
        "9NXQXXLFST89",               // Disney+
        "9PFHDD62MXS1",               // Apple Music
    };

    /// <summary>Version d'essai, payante ensuite.</summary>
    public static readonly HashSet<string> Trial = new(StringComparer.OrdinalIgnoreCase)
    {
        "RARLab.WinRAR",
    };

    /// <summary>Gratuit (au moins dans une formule de base), mais il faut créer un compte.</summary>
    public static readonly HashSet<string> Account = new(StringComparer.OrdinalIgnoreCase)
    {
        // Communication
        "Discord.Discord", "Telegram.TelegramDesktop", "OpenWhisperSystems.Signal", "Microsoft.Teams",
        "SlackTechnologies.Slack", "Element.Element", "Proton.ProtonMail",
        "9NKSQGP7F2NH", "9NBLGGH5L9XT", "9NH2GPH4JZS4",   // WhatsApp, Instagram, TikTok
        // Jeux
        "Valve.Steam", "EpicGames.EpicGamesLauncher", "GOG.Galaxy", "Ubisoft.Connect", "ElectronicArts.EADesktop",
        "RockstarGames.Launcher", "Amazon.Games", "RiotGames.LeagueOfLegends.EUW", "RiotGames.Valorant.EU",
        "Ankama.AnkamaLauncher", "Nvidia.GeForceNow", "Parsec.Parsec", "WeMod.WeMod",
        "9MV0B5HZVK9Z",                                    // Xbox
        // Multimédia et création
        "Spotify.Spotify", "Deezer.Deezer", "Plex.Plex", "Stremio.Stremio",
        "9NM4T8B9JQZ1",                                    // Apple TV
        "Streamlabs.Streamlabs", "Figma.Figma", "Canva.Canva", "MedalB.V.Medal",
        // Bureautique
        "Notion.Notion", "Evernote.Evernote",
        // Intelligence artificielle
        "Anthropic.Claude", "OpenAI.ChatGPT", "Google.GeminiDesktop", "Anthropic.ClaudeCode",
        "9NBLGGH5R558",                                    // Microsoft To Do
        // Sécurité, cloud
        "Bitwarden.Bitwarden", "Proton.ProtonPass", "Proton.ProtonVPN", "Tailscale.Tailscale",
        "Google.GoogleDrive", "Dropbox.Dropbox", "Microsoft.OneDrive", "Mega.MEGASync", "Proton.ProtonDrive",
        "pCloudAG.pCloudDrive",
        "9PKTQ5699M62",                                    // iCloud
        // Développement
        "Anysphere.Cursor", "Codeium.Windsurf",
    };
}
