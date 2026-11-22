using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using CleanSlate.Models;

namespace CleanSlate.Services;

/// <summary>Une ligne du fichier de sélection : un logiciel coché et, s'il y en a un, le dossier choisi pour lui.</summary>
public sealed record SavedApp(string Id, string? Dossier);

/// <summary>
/// Enregistre et relit « ma sélection » dans un petit fichier JSON, à garder sur une clé USB ou à envoyer à un ami.
/// Le fichier ne contient que des identifiants de logiciels et des dossiers : aucun programme, rien de personnel.
/// </summary>
public static class SelectionFile
{
    /// <summary>Filtre du sélecteur de fichier de Windows.</summary>
    public const string Filter = "Sélection Clean Slate (*.json)|*.json";

    private sealed record Document(int Version, List<SavedApp> Logiciels);

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        // Les accents restent lisibles dans le fichier au lieu d'être écrits « é ».
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static void Save(string path, IEnumerable<AppItem> apps)
    {
        var document = new Document(1, [.. apps.Select(a => new SavedApp(a.WingetId, a.InstallFolder))]);
        File.WriteAllText(path, JsonSerializer.Serialize(document, Options));
    }

    /// <exception cref="InvalidDataException">Si le fichier n'est pas une sélection Clean Slate.</exception>
    public static IReadOnlyList<SavedApp> Load(string path)
    {
        try
        {
            var document = JsonSerializer.Deserialize<Document>(File.ReadAllText(path), Options);
            if (document?.Logiciels is not { } apps) throw new InvalidDataException();
            return [.. apps.Where(a => !string.IsNullOrWhiteSpace(a?.Id))];
        }
        catch (JsonException)
        {
            throw new InvalidDataException();
        }
    }
}
