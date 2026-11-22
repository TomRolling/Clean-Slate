using System.Globalization;
using System.Text;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CleanSlate.Models;

/// <summary>Ce qu'il faut pour se servir d'un logiciel une fois installé.</summary>
public enum Access
{
    Free,
    /// <summary>Gratuit, mais il faut créer un compte.</summary>
    Account,
    /// <summary>Version d'essai, payante ensuite.</summary>
    Trial,
    /// <summary>Abonnement ou achat nécessaire.</summary>
    Paid,
}

/// <summary>Un logiciel du catalogue : une tuile à cocher, puis une ligne pendant l'installation.</summary>
public sealed partial class AppItem : ObservableObject
{
    public string Name { get; }

    /// <summary>Identifiant du paquet dans winget (ou, pour une appli du Microsoft Store, son identifiant dans le Store).</summary>
    public string WingetId { get; }

    public string Description { get; }

    /// <summary>
    /// Pour une appli du Microsoft Store : le nom de son paquet Windows (donné par le Store), qui sert à savoir si
    /// elle est déjà installée. Null pour un logiciel classique.
    /// </summary>
    public string? StorePackage { get; }

    public bool IsStoreApp => StorePackage is not null;

    /// <summary>Gratuit, compte à créer, essai ou payant (renseigné par le catalogue d'après CatalogAccess).</summary>
    public Access Access { get; internal set; }

    /// <summary>Petit mot affiché devant la description quand le logiciel n'est pas simplement gratuit.</summary>
    public string AccessLabel => Access switch
    {
        Access.Account => "Compte · ",
        Access.Trial => "Essai · ",
        Access.Paid => "Payant · ",
        _ => "",
    };

    /// <summary>Texte de l'infobulle de la tuile : la description, puis ce que ça coûte.</summary>
    public string Tip => Description + "\n" + Access switch
    {
        Access.Account => "Gratuit, avec un compte à créer.",
        Access.Trial => "Version d'essai : payant ensuite.",
        Access.Paid => "Payant : abonnement ou achat nécessaire.",
        _ => "Gratuit.",
    } + (IsStoreApp ? "\nVient du Microsoft Store." : "")
      + (IsInstalled ? "\nDéjà sur ce PC. Si ce n'est pas le cas, clique pour l'installer quand même." : "");

    /// <summary>Vrai pour les logiciels que presque tout le monde installe : ils forment la première page.</summary>
    public bool IsPopular { get; }

    /// <summary>Famille du logiciel (renseignée par le catalogue).</summary>
    public string Category { get; internal set; } = "";

    /// <summary>Première lettre du nom, affichée dans la pastille de couleur.</summary>
    public string Initial { get; }

    /// <summary>
    /// Adresse du logo embarqué dans l'exécutable, ou null s'il n'y en a pas (la pastille de couleur le remplace).
    /// Renseignée par le catalogue.
    /// </summary>
    public string? LogoUri { get; internal set; }

    /// <summary>Couleur de la pastille : toujours la même pour un nom donné.</summary>
    public Brush Tint { get; }

    /// <summary>Nom, description et famille en minuscules sans accents, pour la recherche.</summary>
    public string SearchText { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InstalledText))]
    public partial bool IsSelected { get; set; }

    /// <summary>
    /// Vrai si le logiciel a été reconnu sur le PC : la tuile est grisée. On peut quand même la cocher
    /// (la détection peut se tromper) : winget l'installera, ou dira qu'il est déjà là.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InstalledText), nameof(Tip))]
    public partial bool IsInstalled { get; set; }

    /// <summary>Phrase de la tuile d'un logiciel reconnu sur le PC.</summary>
    public string InstalledText => IsSelected ? "Sera installé quand même" : "Déjà installé";

    /// <summary>
    /// Vrai si l'installeur accepte qu'on choisisse son dossier (renseigné par le catalogue).
    /// Sinon l'éditeur impose l'emplacement et l'appli ne propose pas le choix.
    /// </summary>
    public bool CanChooseLocation { get; internal set; }

    /// <summary>Dossier choisi par l'utilisateur pour ce logiciel, ou null pour l'emplacement habituel.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TargetFolder), nameof(LocationText), nameof(LocationShort), nameof(HasCustomLocation))]
    public partial string? InstallFolder { get; set; }

    /// <summary>Le dossier où le logiciel ira vraiment : un sous-dossier à son nom dans le dossier choisi.</summary>
    public string? TargetFolder => InstallFolder is null ? null : System.IO.Path.Combine(InstallFolder, FolderName);

    public bool HasCustomLocation => InstallFolder is not null;

    public string LocationText =>
        !CanChooseLocation ? "Emplacement imposé par ce logiciel"
        : TargetFolder ?? "Emplacement habituel";

    /// <summary>
    /// Le même texte, raccourci par le milieu s'il est long : on garde le disque au début et le nom du dossier à la
    /// fin (« D:\Jeux\Lanceur…\Epic Games Launcher »), les deux bouts utiles d'un chemin.
    /// </summary>
    public string LocationShort => ShortenMiddle(LocationText, 40);

    internal static string ShortenMiddle(string text, int max)
    {
        if (text.Length <= max) return text;
        int start = max / 3;
        return text[..start] + "…" + text[^(max - start - 1)..];
    }

    /// <summary>Le nom du logiciel sans les caractères interdits dans un nom de dossier.</summary>
    private string FolderName =>
        string.Concat(Name.Where(c => !System.IO.Path.GetInvalidFileNameChars().Contains(c))).Trim().TrimEnd('.');

    /// <summary>Écran « Où installer ? » : le disque visé et la place qu'il lui reste (« D: · 310 Go libres »).</summary>
    [ObservableProperty] public partial string DriveText { get; set; } = "";

    /// <summary>Vrai quand ce disque est presque plein : le texte passe à l'orange.</summary>
    [ObservableProperty] public partial bool DriveIsLow { get; set; }

    /// <summary>
    /// Raccourci créé par l'installeur (menu Démarrer ou Bureau), trouvé juste après l'installation :
    /// il sert au bouton « Ouvrir ». Null si l'installeur n'en a pas créé.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanLaunch))]
    public partial string? LaunchPath { get; set; }

    public bool CanLaunch => LaunchPath is not null;

    // Pendant l'installation : l'icône d'état et la phrase sous le nom.
    [ObservableProperty] public partial Status Status { get; set; }
    [ObservableProperty] public partial string Detail { get; set; }

    public AppItem(string name, string wingetId, string description, bool isPopular, string? storePackage = null)
    {
        StorePackage = storePackage;
        Name = name;
        WingetId = wingetId;
        Description = description;
        IsPopular = isPopular;
        Detail = "";
        Initial = name.FirstOrDefault(char.IsLetterOrDigit, '?').ToString().ToUpperInvariant();
        Tint = Palette[name.Sum(c => c) % Palette.Length];
        SearchText = Normalize($"{name} {description}");
    }

    /// <summary>Appelé par le catalogue une fois la famille connue, pour qu'on puisse aussi chercher « navigateur ».</summary>
    internal void IndexCategory() => SearchText = Normalize($"{Name} {Description} {Category}");

    /// <summary>Minuscules sans accents : « Éditeur » et « editeur » se trouvent l'un l'autre.</summary>
    public static string Normalize(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (char c in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(c));
        }
        return builder.ToString();
    }

    private static readonly Brush[] Palette = CreatePalette(
        "#4C8DF6", "#8B6CF0", "#E0679A", "#E3873B", "#3FA796", "#5FA84A", "#D25B5B", "#4FA3C7");

    private static Brush[] CreatePalette(params string[] colors) =>
        [.. colors.Select(hex =>
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();   // figé : utilisable depuis n'importe quel thread et plus léger
            return (Brush)brush;
        })];
}
