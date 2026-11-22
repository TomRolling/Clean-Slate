using System.IO;
using System.Text.RegularExpressions;
using CleanSlate.Models;
using Microsoft.Win32;

namespace CleanSlate.Services;

/// <summary>
/// Deuxième vérification de « déjà installé », faite sur le PC lui-même, sans passer par winget.
/// winget ne reconnaît un logiciel que s'il arrive à le relier à sa fiche : il rate par exemple Brave, WinRAR, ou
/// Spotify quand il vient du Microsoft Store. Ici on lit directement :
///   - la liste « Applications installées » de Windows (le registre, clés « Uninstall ») ;
///   - les paquets Windows du compte (dossiers de « AppData\Local\Packages »), pour les versions du Store.
/// Un logiciel est reconnu si son nom y figure tel quel (numéro de version mis de côté), ou sous un des autres noms
/// notés à la main dans <see cref="OtherNames"/> et <see cref="Packages"/>. Pas de comparaison approximative :
/// « Opera » ne doit pas être grisé parce que « Opera GX » est installé.
/// </summary>
public sealed partial class InstalledApps
{
    private readonly HashSet<string> _names = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _packages = new(StringComparer.OrdinalIgnoreCase);

    /// <param name="programNames">Noms affichés dans « Applications installées ».</param>
    /// <param name="packageFolders">Noms des paquets Windows présents (« SpotifyAB.SpotifyMusic_zpdnekdrzrea0 »).</param>
    public InstalledApps(IEnumerable<string> programNames, IEnumerable<string> packageFolders)
    {
        foreach (string name in programNames)
        {
            _names.Add(Spaces().Replace(name, " ").Trim());   // le nom exact…
            _names.Add(Simplify(name));                        // …et le nom sans version ni « (64-bit) »
        }
        _names.Remove("");
        foreach (string package in packageFolders) _packages.Add(package);
    }

    /// <summary>Lit ce qui est installé sur ce PC. Ne lève pas d'exception : au pire, la liste est incomplète.</summary>
    public static InstalledApps Scan()
    {
        var names = new List<string>();
        // Trois endroits : logiciels 64 bits du PC, logiciels 32 bits du PC, logiciels installés pour ce compte seulement.
        (RegistryHive Hive, RegistryView View)[] places =
        [
            (RegistryHive.LocalMachine, RegistryView.Registry64),
            (RegistryHive.LocalMachine, RegistryView.Registry32),
            (RegistryHive.CurrentUser, RegistryView.Default),
        ];
        foreach (var (hive, view) in places)
        {
            try
            {
                using var root = RegistryKey.OpenBaseKey(hive, view);
                using var uninstall = root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                if (uninstall is null) continue;
                foreach (string keyName in uninstall.GetSubKeyNames())
                {
                    using var key = uninstall.OpenSubKey(keyName);
                    if (key?.GetValue("DisplayName") is string name && name.Length > 0) names.Add(name);
                }
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
            {
                Logger.Error("Lecture des applications installées (registre)", ex);
            }
        }

        var packages = new List<string>();
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages");
            if (Directory.Exists(folder))
                packages.AddRange(Directory.EnumerateDirectories(folder).Select(d => Path.GetFileName(d)));
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            Logger.Error("Lecture des paquets Windows", ex);
        }
        return new InstalledApps(names, packages);
    }

    /// <summary>Vrai si ce logiciel du catalogue est présent sur le PC, sous une forme ou une autre.</summary>
    public bool Contains(AppItem app)
    {
        if (app.StorePackage is { } storePackage) return _packages.Contains(storePackage);
        if (_names.Contains(app.Name)) return true;
        if (OtherNames.TryGetValue(app.WingetId, out var names) && names.Any(_names.Contains)) return true;
        return Packages.TryGetValue(app.WingetId, out var packages) && packages.Any(_packages.Contains);
    }

    /// <summary>
    /// Retire d'un nom ce qui change d'une version à l'autre : « WinRAR 7.23 (64-bit) » devient « WinRAR »,
    /// « Stremio version 5.0.26 » devient « Stremio ». Un nombre seul est gardé (« HWiNFO 64 », « 7-Zip »).
    /// </summary>
    internal static string Simplify(string name)
    {
        name = Parentheses().Replace(name, " ");
        name = name.Replace("®", "").Replace("™", "").Replace("©", "");
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => !VersionNumber().IsMatch(w) && !Noise.Contains(w));
        return string.Join(' ', words).Trim(' ', '-');
    }

    private static readonly HashSet<string> Noise = new(StringComparer.OrdinalIgnoreCase)
        { "version", "release", "x64", "x86", "64-bit", "32-bit" };

    [GeneratedRegex(@"\([^)]*\)")] private static partial Regex Parentheses();
    [GeneratedRegex(@"^v?\d+(\.\d+)+[-+.\w]*$", RegexOptions.IgnoreCase)] private static partial Regex VersionNumber();
    [GeneratedRegex(@"\s+")] private static partial Regex Spaces();

    /// <summary>
    /// Quand Windows n'affiche pas le logiciel sous le nom du catalogue : identifiant winget → noms à reconnaître
    /// (déjà simplifiés, voir <see cref="Simplify"/>). Liste tenue à la main, à compléter au fil des cas rencontrés.
    /// </summary>
    private static readonly Dictionary<string, string[]> OtherNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Microsoft.VisualStudioCode"] = ["Microsoft Visual Studio Code"],
        ["Microsoft.VisualStudio.2022.Community"] = ["Visual Studio Community 2022"],
        ["VideoLAN.VLC"] = ["VLC media player"],
        ["Opera.Opera"] = ["Opera Stable"],
        ["Opera.OperaGX"] = ["Opera GX Stable"],
        ["Telegram.TelegramDesktop"] = ["Telegram Desktop"],
        ["Zoom.Zoom"] = ["Zoom Workplace"],
        ["Adobe.Acrobat.Reader.64-bit"] = ["Adobe Acrobat"],
        ["OpenJS.NodeJS.LTS"] = ["Node.js"],
        ["REALiX.HWiNFO"] = ["HWiNFO 64"],
        ["Moonsworth.LunarClient"] = ["Uninstall Lunar Client"],   // c'est bien le nom que son installeur inscrit
        ["Corsair.iCUE.5"] = ["Corsair iCUE5 Software"],
        ["Microsoft.XNARedist"] = ["Microsoft XNA Framework Redistributable Refresh"],
        ["Nvidia.PhysX"] = ["NVIDIA Logiciel système PhysX", "NVIDIA PhysX System Software"],
    };

    /// <summary>
    /// Logiciels qui existent aussi en paquet Windows (version du Microsoft Store le plus souvent) : identifiant
    /// winget → noms de paquet. Noms relevés dans la fiche du Store ou sur un PC où le logiciel est installé.
    /// </summary>
    private static readonly Dictionary<string, string[]> Packages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Spotify.Spotify"] = ["SpotifyAB.SpotifyMusic_zpdnekdrzrea0"],
        ["Anthropic.Claude"] = ["Claude_pzs8sxrjxfjjc", "AnthropicPBC.Claude_fnn82j28hfe8t"],
        ["OpenAI.ChatGPT"] = ["OpenAI.Codex_2p2nqsd0c76g0"],
        ["Notion.Notion"] = ["com.notion.app.desktop.notion_87smfmv0eebam"],
        ["Apple.iTunes"] = ["AppleInc.iTunes_nzyj5cx40ttqa"],
        ["Telegram.TelegramDesktop"] = ["TelegramMessengerLLP.TelegramDesktop_t4vj0pshhgkwm"],
        ["SlackTechnologies.Slack"] = ["91750D7E.Slack_8she8kybcnzg4"],
        ["Mozilla.Firefox"] = ["Mozilla.Firefox_n80bbvh6b1yt2"],
    };
}
