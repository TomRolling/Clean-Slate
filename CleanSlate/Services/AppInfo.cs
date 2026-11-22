using System.IO;

namespace CleanSlate.Services;

/// <summary>Le nom de l'appli et ses dossiers de données, à un seul endroit.</summary>
public static class AppInfo
{
    /// <summary>Nom affiché à l'écran.</summary>
    public const string Name = "Clean Slate";

    /// <summary>Nom des dossiers de données (sans espace).</summary>
    public const string FolderName = "Clean Slate";

    /// <summary>Dossier privé de l'appli pour ce compte : journal et téléchargements.</summary>
    public static string LocalFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FolderName);

    /// <summary>Dossier commun à tout le PC : fichier de sortie d'un winget lancé sans droits d'administrateur.</summary>
    public static string CommonFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), FolderName);
}
