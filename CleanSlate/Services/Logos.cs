using System.Collections;
using System.Resources;
using System.Text.RegularExpressions;

namespace CleanSlate.Services;

/// <summary>
/// Les logos des logiciels, embarqués dans l'exécutable (dossier Assets\Logos, rempli par telecharger-logos.ps1).
/// Rien n'est téléchargé pendant que l'appli tourne.
/// </summary>
public static partial class Logos
{
    // Les images embarquées par WPF sont rangées dans une ressource « <programme>.g.resources »,
    // sous leur chemin en minuscules : on en lit la liste une fois.
    private static readonly HashSet<string> Embedded = ReadEmbedded();

    /// <summary>Adresse « pack » du logo de ce logiciel, ou null s'il n'en a pas.</summary>
    public static string? Find(string wingetId)
    {
        // Même règle de nommage que telecharger-logos.ps1.
        string file = SpecialCharacters().Replace(wingetId, "_") + ".png";
        return Embedded.Contains($"assets/logos/{file}".ToLowerInvariant())
            ? $"pack://application:,,,/CleanSlate;component/Assets/Logos/{file}"
            : null;
    }

    private static HashSet<string> ReadEmbedded()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var assembly = typeof(Logos).Assembly;
        using var stream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.g.resources");
        if (stream is null) return names;

        using var reader = new ResourceReader(stream);
        foreach (DictionaryEntry entry in reader)
        {
            if (entry.Key is string name) names.Add(Uri.UnescapeDataString(name));
        }
        return names;
    }

    [GeneratedRegex("[^A-Za-z0-9.-]")]
    private static partial Regex SpecialCharacters();
}
