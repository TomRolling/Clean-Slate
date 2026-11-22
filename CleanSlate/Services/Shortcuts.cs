using System.Diagnostics;
using System.IO;
using CleanSlate.Models;

namespace CleanSlate.Services;

/// <summary>
/// Sert au bouton « Ouvrir » de la fin d'installation. winget ne dit pas où est le programme installé ; en revanche
/// presque tous les installeurs créent un raccourci dans le menu Démarrer ou sur le Bureau. On note donc les
/// raccourcis présents avant l'installation, et celui qui apparaît après est celui du logiciel.
/// </summary>
public static class Shortcuts
{
    /// <summary>Tous les raccourcis du menu Démarrer et du Bureau (ceux du PC et ceux du compte).</summary>
    public static HashSet<string> Snapshot()
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Environment.SpecialFolder[] folders =
        [
            Environment.SpecialFolder.CommonPrograms, Environment.SpecialFolder.Programs,
            Environment.SpecialFolder.CommonDesktopDirectory, Environment.SpecialFolder.DesktopDirectory,
        ];
        foreach (var folder in folders)
        {
            string path = Environment.GetFolderPath(folder);
            if (path.Length == 0 || !Directory.Exists(path)) continue;
            try
            {
                var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };
                foreach (string file in Directory.EnumerateFiles(path, "*.lnk", options)) found.Add(file);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Dossier illisible : tant pis, le bouton « Ouvrir » ne sera simplement pas proposé.
            }
        }
        return found;
    }

    // Raccourcis qu'un installeur ajoute à côté du vrai : on ne veut pas ouvrir le désinstalleur ou la notice.
    private static readonly string[] NotTheApp =
        ["uninstall", "desinstall", "supprimer", "remove", "readme", "lisezmoi", "help", "aide", "manual", "license",
         "licence", "website", "site web", "documentation", "changelog", "release notes", "safe mode", "mode sans echec"];

    /// <summary>Choisit, parmi des raccourcis, celui dont le nom ressemble le plus à celui du logiciel.</summary>
    public static string? Choose(IEnumerable<string> shortcuts, string appName)
    {
        string[] words = AppItem.Normalize(appName).Split([' ', '-', '.', '+'], StringSplitOptions.RemoveEmptyEntries);
        return shortcuts
            .Select(file => (File: file, Name: AppItem.Normalize(Path.GetFileNameWithoutExtension(file))))
            .Where(s => !NotTheApp.Any(s.Name.Contains))
            // D'abord celui qui reprend le plus de mots du nom du logiciel, puis le nom le plus court (« Krita » avant « Krita (debug) »).
            .OrderByDescending(s => words.Count(s.Name.Contains))
            .ThenBy(s => s.Name.Length)
            .Select(s => s.File)
            .FirstOrDefault();
    }

    /// <summary>
    /// Ouvre un raccourci. L'appli tourne en administrateur, mais le logiciel ouvert ne doit pas l'être : on le lance
    /// avec le compte normal, comme si l'utilisateur avait cliqué dessus dans le menu Démarrer.
    /// </summary>
    public static void Open(string shortcut)
    {
        try
        {
            if (Environment.IsPrivilegedProcess && StandardUser.TryOpen(shortcut)) return;
            Process.Start(new ProcessStartInfo(shortcut) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or IOException or InvalidOperationException)
        {
            Logger.Error("Ouverture d'un logiciel installé", ex);
        }
    }
}
