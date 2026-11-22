using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CleanSlate.Models;
using CleanSlate.Services;

namespace CleanSlate.ViewModels;

/// <summary>Une entrée de la barre latérale : elle choisit quels logiciels sont affichés.</summary>
public sealed partial class CatalogFilter(string title, string icon, Func<AppItem, bool> match) : ObservableObject
{
    public string Title { get; } = title;

    /// <summary>Caractère de la police d'icônes Windows.</summary>
    public string Icon { get; } = icon;

    public Func<AppItem, bool> Match { get; } = match;

    /// <summary>Nombre de logiciels cochés dans cette entrée (vide s'il n'y en a aucun).</summary>
    [ObservableProperty] public partial string Badge { get; set; } = "";
}

/// <summary>Un pack affiché en haut de la première page : un clic coche tous ses logiciels.</summary>
public sealed partial class Pack(PackDefinition definition, IReadOnlyList<AppItem> apps) : ObservableObject
{
    public string Name { get; } = definition.Name;
    public string Icon { get; } = definition.Icon;
    public IReadOnlyList<AppItem> Apps { get; } = apps;

    /// <summary>Infobulle : à quoi sert le pack, et ce qu'il contient.</summary>
    public string Tip { get; } = $"{definition.Description} :\n{string.Join(", ", apps.Select(a => a.Name))}";

    /// <summary>Vrai quand tout le pack est coché (ou déjà installé) : la pastille s'allume.</summary>
    [ObservableProperty] public partial bool IsActive { get; set; }
}

/// <summary>Les trois écrans, dans l'ordre : on coche, on vérifie où ça s'installe, on regarde l'installation.</summary>
public enum Step
{
    Catalogue,
    Review,
    Install,
}

/// <summary>Un bloc de tuiles sous un titre de famille.</summary>
public sealed record AppGroup(string Title, IReadOnlyList<AppItem> Apps);

/// <summary>
/// Tout l'écran : le catalogue à cocher, puis le suivi de l'installation.
/// La logique est simple : on coche, on clique sur « Installer », winget installe les logiciels un par un.
/// </summary>
public sealed partial class CatalogueViewModel : PageViewModel
{
    private readonly IReadOnlyList<CatalogCategory> _categories = Catalog.Create();
    private readonly List<AppItem> _apps;
    private readonly CatalogFilter _selectionFilter;

    public IReadOnlyList<CatalogFilter> Filters { get; }

    public IReadOnlyList<Pack> Packs { get; }

    /// <summary>Les packs ne s'affichent que sur la première page, hors recherche.</summary>
    [ObservableProperty] public partial bool ShowPacks { get; set; }

    /// <summary>Lien sous la recherche : « 23 déjà installés », ou « Retour au catalogue » quand on les regarde.</summary>
    [ObservableProperty] public partial string InstalledLink { get; set; } = "";

    // Vrai quand on affiche seulement ce qui est déjà sur le PC (clic sur le lien ci-dessus).
    private bool _installedOnly;

    // Les logiciels de la dernière installation qui n'ont pas abouti : ce que « Réessayer » relance.
    private List<AppItem> _notDone = [];

    // Phrase affichée une fois en haut de « Ma sélection », après l'ouverture d'un fichier de sélection.
    private string? _notice;

    /// <summary>Les logiciels de l'installation en cours (ou de la dernière), dans l'ordre où ils passent.</summary>
    public ObservableCollection<AppItem> Queue { get; } = [];

    /// <summary>Ceux de la file dont on peut choisir le dossier : les lignes de l'écran « Où installer ? ».</summary>
    public ObservableCollection<AppItem> Choosable { get; } = [];

    public LogViewModel Log { get; } = new();

    [ObservableProperty] public partial CatalogFilter CurrentFilter { get; set; }
    [ObservableProperty] public partial string SearchText { get; set; } = "";
    [ObservableProperty] public partial IReadOnlyList<AppGroup> Groups { get; set; } = [];

    /// <summary>Titre au-dessus des tuiles : le nom de la famille, ou « Résultats pour … ».</summary>
    [ObservableProperty] public partial string PageTitle { get; set; } = "";

    /// <summary>Phrase en gris sous le titre.</summary>
    [ObservableProperty] public partial string PageHint { get; set; } = "";

    /// <summary>Message affiché quand aucune tuile ne correspond (vide sinon).</summary>
    [ObservableProperty] public partial string EmptyText { get; set; } = "";

    [ObservableProperty] public partial string SelectionText { get; set; } = "";

    /// <summary>L'écran affiché.</summary>
    [ObservableProperty] public partial Step Step { get; set; }

    /// <summary>Titre et phrase de l'écran « Où installer ? ».</summary>
    [ObservableProperty] public partial string ReviewTitle { get; set; } = "";
    [ObservableProperty] public partial string ReviewHint { get; set; } = "";

    /// <summary>Phrase sur les logiciels dont l'emplacement est imposé par l'éditeur (vide s'il n'y en a pas).</summary>
    [ObservableProperty] public partial string FixedText { get; set; } = "";

    /// <summary>Place libre sur le disque de Windows, rappelée en bas de l'écran « Où installer ? ».</summary>
    [ObservableProperty] public partial string DiskText { get; set; } = "";
    [ObservableProperty] public partial bool DiskIsLow { get; set; }

    /// <summary>Texte du bouton qui copie le résumé de l'installation (il confirme la copie un instant).</summary>
    [ObservableProperty] public partial string CopyLabel { get; set; } = CopyLabelIdle;
    private const string CopyLabelIdle = "Copier le résumé";

    [ObservableProperty] public partial bool ShowLog { get; set; }
    [ObservableProperty] public partial bool HasFailures { get; set; }

    /// <summary>Message affiché quand winget est absent (vide sinon).</summary>
    [ObservableProperty] public partial string WingetWarning { get; set; } = "";

    public CatalogueViewModel() : base("Catalogue", "", "", "", ringText: "RETOUR", ringCaption: "à la liste")
    {
        _apps = [.. _categories.SelectMany(c => c.Apps)];

        _selectionFilter = new CatalogFilter("Ma sélection", "", a => a.IsSelected);
        Filters =
        [
            new CatalogFilter("Les indispensables", "", a => a.IsPopular),
            new CatalogFilter("Tout le catalogue", "", _ => true),
            .. _categories.Select(c => new CatalogFilter(c.Name, c.Icon, a => a.Category == c.Name)),
            _selectionFilter,
        ];

        var byId = _apps.ToDictionary(a => a.WingetId, StringComparer.OrdinalIgnoreCase);
        Packs = [.. Catalog.Packs.Select(p => new Pack(p, [.. p.Ids.Select(id => byId[id])]))];

        foreach (var app in _apps) app.PropertyChanged += OnAppChanged;

        CurrentFilter = Filters[0];
        UpdateSelection();
    }

    // ───────────────────────── Affichage du catalogue ─────────────────────────

    partial void OnCurrentFilterChanged(CatalogFilter value)
    {
        if (value != _selectionFilter) _notice = null;
        _installedOnly = false;
        UpdateInstalledLink();

        // Changer de famille efface la recherche : on voit toujours ce sur quoi on vient de cliquer.
        if (SearchText.Length > 0) SearchText = "";
        else RebuildGroups();
    }

    partial void OnSearchTextChanged(string value)
    {
        // Chercher fait sortir de la page « déjà installés » : une recherche porte toujours sur tout le catalogue.
        if (value.Length > 0 && _installedOnly)
        {
            _installedOnly = false;
            UpdateInstalledLink();
        }
        RebuildGroups();
    }

    /// <summary>Clic sur « 23 déjà installés » : montre ces logiciels-là, un second clic revient au catalogue.</summary>
    [RelayCommand]
    private void ToggleInstalledView()
    {
        _installedOnly = !_installedOnly;
        UpdateInstalledLink();
        if (SearchText.Length > 0) SearchText = "";
        else RebuildGroups();
    }

    private void UpdateInstalledLink()
    {
        int count = _apps.Count(a => a.IsInstalled);
        InstalledLink = _installedOnly ? "Retour au catalogue"
            : count == 0 ? ""
            : Plural(count, "déjà installé", "déjà installés");
    }

    private void RebuildGroups()
    {
        if (CurrentFilter is null) return;

        string search = AppItem.Normalize(SearchText.Trim());
        bool searching = search.Length > 0;

        // Une recherche porte sur tout le catalogue, quelle que soit la famille ouverte.
        bool installedOnly = _installedOnly && !searching;
        Func<AppItem, bool> match = searching ? a => a.SearchText.Contains(search)
            : installedOnly ? a => a.IsInstalled
            : CurrentFilter.Match;

        Groups = [.. _categories
            .Select(c => new AppGroup(c.Name, [.. c.Apps.Where(match)]))
            .Where(g => g.Apps.Count > 0)];

        ShowPacks = !searching && !installedOnly && CurrentFilter == Filters[0];

        int count = Groups.Sum(g => g.Apps.Count);
        PageTitle = searching ? $"Résultats pour « {SearchText.Trim()} »" : installedOnly ? "Déjà sur ce PC" : CurrentFilter.Title;
        PageHint = searching ? Plural(count, "logiciel trouvé", "logiciels trouvés")
            : installedOnly ? "Ce que l'appli a reconnu sur ton PC. Si l'un d'eux n'y est pas vraiment, clique dessus pour l'installer quand même."
            : CurrentFilter == Filters[0] ? "Ce que presque tout le monde installe sur un PC neuf. Coche ce que tu veux, puis clique sur « Suivant »."
            : CurrentFilter == _selectionFilter ? _notice ?? "Tout ce que tu as coché, toutes familles confondues."
            : Plural(count, "logiciel", "logiciels");
        EmptyText = count > 0 ? ""
            : searching ? "Aucun logiciel ne correspond à cette recherche."
            : CurrentFilter == _selectionFilter ? "Rien de coché pour l'instant. Choisis une famille à gauche et clique sur les logiciels que tu veux."
            : "";
    }

    private void OnAppChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AppItem.IsSelected) or nameof(AppItem.IsInstalled)) UpdateSelection();
    }

    /// <summary>
    /// Les logiciels cochés. Un logiciel reconnu sur le PC est décoché d'office, mais on peut le recocher :
    /// c'est le « installer quand même », pour le cas où la détection se trompe.
    /// </summary>
    private IEnumerable<AppItem> Selected => _apps.Where(a => a.IsSelected);

    private void UpdateSelection()
    {
        int count = Selected.Count();
        SelectionText = count == 0 ? "Aucun logiciel coché" : Plural(count, "logiciel coché", "logiciels cochés");

        foreach (var filter in Filters)
        {
            // Le badge compte ce qui est coché dans la famille ; inutile sur les deux pages qui mélangent tout.
            bool counted = filter == _selectionFilter || _categories.Any(c => c.Name == filter.Title);
            int selected = counted ? Selected.Count(a => filter == _selectionFilter || filter.Match(a)) : 0;
            filter.Badge = selected > 0 ? selected.ToString() : "";
        }

        // Un pack est « allumé » quand il ne lui reste rien à cocher.
        foreach (var pack in Packs) pack.IsActive = pack.Apps.All(a => a.IsSelected || a.IsInstalled);

        InstallCommand.NotifyCanExecuteChanged();
        ClearSelectionCommand.NotifyCanExecuteChanged();
        SaveSelectionCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Clic sur un pack : coche tous ses logiciels, ou les décoche s'ils l'étaient déjà tous.</summary>
    [RelayCommand]
    private void TogglePack(Pack? pack)
    {
        if (pack is null) return;
        var available = pack.Apps.Where(a => !a.IsInstalled).ToList();
        bool select = available.Any(a => !a.IsSelected);
        foreach (var app in available) app.IsSelected = select;
    }

    /// <summary>Bouton « Tout cocher » d'une famille : coche tout, ou décoche tout si tout l'était déjà.</summary>
    [RelayCommand]
    private void ToggleGroup(AppGroup? group)
    {
        if (group is null) return;
        var available = group.Apps.Where(a => !a.IsInstalled).ToList();
        bool select = available.Any(a => !a.IsSelected);
        foreach (var app in available) app.IsSelected = select;
    }

    private bool HasSelection() => Selected.Any();

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void ClearSelection()
    {
        foreach (var app in _apps) app.IsSelected = false;
        if (CurrentFilter == _selectionFilter) RebuildGroups();
    }

    // ───────────────────────── Enregistrer / rouvrir sa sélection ─────────────────────────

    /// <summary>Écrit les logiciels cochés (et les dossiers choisis) dans un fichier, pour une prochaine fois ou pour un ami.</summary>
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void SaveSelection()
    {
        if (Dialogs.PickSaveFile?.Invoke("Ma sélection Clean Slate", SelectionFile.Filter) is not { } path) return;
        try
        {
            SelectionFile.Save(path, Selected);
            ShowNotice($"Sélection enregistrée dans {path}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Logger.Error("Enregistrement de la sélection", ex);
            ShowNotice("Impossible d'écrire le fichier à cet endroit. Choisis un autre dossier.");
        }
    }

    [RelayCommand]
    private void OpenSelection()
    {
        if (Dialogs.PickOpenFile?.Invoke(SelectionFile.Filter) is { } path) LoadSelection(path);
    }

    /// <summary>Recoche ce que contient un fichier de sélection (en plus de ce qui est déjà coché).</summary>
    public void LoadSelection(string path)
    {
        IReadOnlyList<SavedApp> saved;
        try
        {
            saved = SelectionFile.Load(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            ShowNotice("Ce fichier n'est pas une sélection Clean Slate, ou il est illisible.");
            return;
        }

        int added = 0, already = 0, unknown = 0;
        foreach (var entry in saved)
        {
            var app = _apps.FirstOrDefault(a => a.WingetId.Equals(entry.Id, StringComparison.OrdinalIgnoreCase));
            if (app is null) unknown++;
            else if (app.IsInstalled) already++;
            else
            {
                app.IsSelected = true;
                // Le fichier peut venir de quelqu'un d'autre : on ne reprend un dossier que s'il a la forme d'un vrai chemin.
                if (app.CanChooseLocation && IsUsableFolder(entry.Dossier)) app.InstallFolder = entry.Dossier;
                added++;
            }
        }

        string text = Plural(added, "logiciel coché", "logiciels cochés") + " depuis le fichier.";
        if (already > 0) text += $" {already} déjà sur ce PC.";
        if (unknown > 0) text += $" {unknown} absent{(unknown > 1 ? "s" : "")} du catalogue de cette version.";
        ShowNotice(text);
    }

    private static bool IsUsableFolder(string? folder) =>
        !string.IsNullOrWhiteSpace(folder) && !folder.Contains('"') && Path.IsPathFullyQualified(folder)
        && folder.IndexOfAny(Path.GetInvalidPathChars()) < 0;

    /// <summary>Ouvre « Ma sélection » avec une phrase d'information à la place de la phrase habituelle.</summary>
    private void ShowNotice(string text)
    {
        _notice = text;
        if (CurrentFilter == _selectionFilter && SearchText.Length == 0) RebuildGroups();
        else if (CurrentFilter == _selectionFilter) SearchText = "";
        else CurrentFilter = _selectionFilter;
    }

    // ───────────────────────── Au lancement : winget et logiciels déjà là ─────────────────────────

    /// <summary>
    /// Appelé une fois à l'ouverture : vérifie que winget est là et lui demande ce qui est déjà installé,
    /// pour griser ces tuiles. Rien n'est téléchargé ni modifié.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            string? version = await Winget.GetVersionAsync(AppLifetime.Token);
            if (version is null)
            {
                WingetWarning = "winget, l'outil de Windows qui télécharge les logiciels, est introuvable sur ce PC. " +
                                "Clique sur « Activer winget », puis sur « Revérifier ».";
                InstallCommand.NotifyCanExecuteChanged();
                return;
            }
            WingetWarning = "";
            InstallCommand.NotifyCanExecuteChanged();
            Logger.Info($"winget {version}");

            // Deux vérifications, l'une complète l'autre : ce que winget reconnaît, et ce que Windows lui-même
            // liste comme installé (winget rate Brave, WinRAR, la version Store de Spotify…).
            var installed = await Winget.ListInstalledAsync(AppLifetime.Token);
            var onThisPc = await Task.Run(InstalledApps.Scan, AppLifetime.Token);
            foreach (var app in _apps)
            {
                bool seenByWinget = !app.IsStoreApp && installed.Contains(app.WingetId);
                app.IsInstalled = seenByWinget || onThisPc.Contains(app);
                if (app.IsInstalled && !seenByWinget && !app.IsStoreApp) Logger.Info($"Déjà installé (vu sur le PC, pas par winget) : {app.Name}");
                if (app.IsInstalled) app.IsSelected = false;
            }
            UpdateInstalledLink();
        }
        catch (OperationCanceledException)
        {
            // L'appli se ferme.
        }
        catch (Exception ex)
        {
            Logger.Error("Démarrage du catalogue", ex);
        }
    }

    [RelayCommand]
    private Task RecheckWingetAsync() => InitializeAsync();

    /// <summary>
    /// Sur un Windows tout neuf, winget est fourni mais pas encore « activé » pour le compte : cette commande,
    /// documentée par Microsoft, l'enregistre. Si cela ne suffit pas, on ouvre sa page du Microsoft Store.
    /// </summary>
    [RelayCommand]
    private async Task EnableWingetAsync()
    {
        try
        {
            await ProcessRunner.RunAsync(ProcessRunner.SystemTool(@"WindowsPowerShell\v1.0\powershell.exe"),
                "-NoProfile -NonInteractive -Command \"Add-AppxPackage -RegisterByFamilyName -MainPackage Microsoft.DesktopAppInstaller_8wekyb3d8bbwe\"",
                null, AppLifetime.Token);
            await InitializeAsync();
            if (WingetWarning.Length == 0) return;

            // « Programme d'installation d'application » : le paquet du Microsoft Store qui contient winget.
            Process.Start(new ProcessStartInfo("ms-windows-store://pdp/?ProductId=9NBLGGH4NNS1") { UseShellExecute = true });
        }
        catch (OperationCanceledException)
        {
            // L'appli se ferme.
        }
        catch (Exception ex)
        {
            Logger.Error("Activation de winget", ex);
        }
    }

    // ───────────────────────── Installation ─────────────────────────

    private bool CanInstall() => !IsBusy && WingetWarning.Length == 0 && Selected.Any();

    /// <summary>Bouton « Suivant » du catalogue : on passe par l'écran « Où installer ? » avant d'installer.</summary>
    [RelayCommand(CanExecute = nameof(CanInstall))]
    private void Install()
    {
        // Les composants système passent en premier : les logiciels en ont besoin pour démarrer.
        var items = Selected.OrderBy(a => a.Category == Catalog.ComponentsCategory ? 0 : 1).ToList();

        Queue.Clear();
        Choosable.Clear();
        foreach (var item in items)
        {
            item.Status = Status.None;
            item.Detail = "";
            Queue.Add(item);
            if (item.CanChooseLocation) Choosable.Add(item);
        }

        var imposed = items.Where(a => !a.CanChooseLocation).ToList();
        string names = string.Join(", ", imposed.Take(6).Select(a => a.Name)) + (imposed.Count > 6 ? "…" : "");
        FixedText = imposed.Count == 0 ? ""
            : imposed.Count == 1 ? $"{names} s'installe à l'emplacement prévu par son éditeur : il ne laisse pas le choix."
            : $"{imposed.Count} autres logiciels s'installent à l'emplacement prévu par leur éditeur, qui ne laisse pas le choix : {names}";

        UpdateReview();
        Step = Step.Review;
    }

    private void UpdateReview()
    {
        ReviewTitle = Choosable.Count == 0 ? $"Prêt à installer {Plural(Queue.Count, "logiciel", "logiciels")}" : "Où installer ?";
        ReviewHint = Choosable.Count == 0
            ? "Ces logiciels choisissent eux-mêmes leur emplacement : il n'y a rien à régler. Clique sur « Lancer l'installation »."
            : "Chaque logiciel va à son emplacement habituel. Pour en mettre un ailleurs (un autre disque par exemple), clique sur son emplacement.";
        ChooseFolderForAllCommand.NotifyCanExecuteChanged();
        ResetFoldersCommand.NotifyCanExecuteChanged();
        UpdateDisks();
    }

    /// <summary>
    /// Place libre sur le disque visé par chaque logiciel. winget ne donne pas la taille des logiciels : on ne peut
    /// donc pas dire « il manque 3 Go », seulement prévenir quand un disque est presque plein.
    /// </summary>
    private void UpdateDisks()
    {
        foreach (var app in Choosable)
        {
            // Sans dossier choisi, l'emplacement habituel est sur le disque de Windows.
            (app.DriveText, app.DriveIsLow) = Disks.Describe(app.TargetFolder ?? Disks.SystemDrive);
        }

        string letter = Disks.SystemDrive.TrimEnd('\\');
        long? free = Disks.FreeBytes(Disks.SystemDrive);
        DiskIsLow = free < Disks.LowSpace;
        DiskText = free is null ? ""
            : DiskIsLow ? $"Attention, le disque de Windows ({letter}) est presque plein : {Format.Size(free.Value)} libres. " +
                          "Tout ce qui n'a pas de dossier choisi s'installe dessus."
            : $"Disque de Windows ({letter}) : {Format.Size(free.Value)} libres";
    }

    /// <summary>Ouvre le sélecteur de dossier de Windows pour un logiciel. Il ira dans un sous-dossier à son nom.</summary>
    [RelayCommand]
    private void ChooseFolder(AppItem? app)
    {
        if (app is null || !app.CanChooseLocation) return;
        if (Dialogs.PickFolder?.Invoke($"Où installer {app.Name} ?") is { } folder) app.InstallFolder = folder;
        UpdateReview();
    }

    [RelayCommand]
    private void ResetFolder(AppItem? app)
    {
        if (app is not null) app.InstallFolder = null;
        UpdateReview();
    }

    private bool AnyChoosable() => Choosable.Count > 0;

    /// <summary>Un seul dossier pour tous les logiciels qui l'acceptent (chacun dans son sous-dossier).</summary>
    [RelayCommand(CanExecute = nameof(AnyChoosable))]
    private void ChooseFolderForAll()
    {
        if (Dialogs.PickFolder?.Invoke("Dossier pour tous les logiciels qui l'acceptent") is not { } folder) return;
        foreach (var app in Choosable) app.InstallFolder = folder;
        UpdateReview();
    }

    private bool AnyCustomFolder() => Queue.Any(a => a.HasCustomLocation);

    [RelayCommand(CanExecute = nameof(AnyCustomFolder))]
    private void ResetFolders()
    {
        foreach (var app in Queue) app.InstallFolder = null;
        UpdateReview();
    }

    /// <summary>Bouton « Lancer l'installation » de l'écran « Où installer ? ».</summary>
    [RelayCommand]
    private Task StartInstallAsync() => RunInstallAsync([.. Queue]);

    [RelayCommand]
    private Task RetryFailedAsync() => RunInstallAsync([.. _notDone]);

    private async Task RunInstallAsync(List<AppItem> items)
    {
        if (IsBusy || items.Count == 0) return;

        Queue.Clear();
        foreach (var item in items)
        {
            item.Status = Status.None;
            item.Detail = "En attente";
            Queue.Add(item);
        }
        foreach (var item in items) item.LaunchPath = null;
        HasFailures = false;
        CopyLabel = CopyLabelIdle;
        Step = Step.Install;
        Log.Clear();

        var token = BeginOperation("Installation en cours…", "Préparation…");
        InstallCommand.NotifyCanExecuteChanged();
        var output = new Progress<string>(Log.Add);
        int succeeded = 0, failures = 0, misplaced = 0;
        var finished = new HashSet<AppItem>();
        try
        {
            // Catalogue de winget rafraîchi d'abord : chaque logiciel est installé dans sa dernière version publiée.
            await Winget.RefreshCatalogAsync(output, token);

            // Les raccourcis déjà là, pour reconnaître ensuite ceux que chaque installeur ajoute (bouton « Ouvrir »).
            var shortcuts = await Task.Run(Shortcuts.Snapshot, token);

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                Report(i * 100.0 / items.Count, $"{i + 1} sur {items.Count} : {item.Name}");
                item.Status = Status.Working;
                item.Detail = "Téléchargement et installation…";

                // winget annonce le téléchargement par une ligne, puis passe à la suite quand il est fini :
                // la phrase sous le nom du logiciel suit ces deux moments.
                bool downloading = false;
                var itemOutput = new Progress<string>(line =>
                {
                    Log.Add(line);
                    if (item.Status != Status.Working) return;
                    if (Winget.IsDownloadLine(line))
                    {
                        downloading = true;
                        item.Detail = "Téléchargement…";
                    }
                    else if (downloading)
                    {
                        downloading = false;
                        item.Detail = "Téléchargé. Installation…";
                    }
                });

                string? target = item.CanChooseLocation ? item.TargetFolder : null;
                var (outcome, code) = await Winget.InstallAsync(item.WingetId, item.IsStoreApp, item.Name, target, itemOutput, token);

                var after = await Task.Run(Shortcuts.Snapshot, token);
                string? shortcut = Shortcuts.Choose(after.Where(f => !shortcuts.Contains(f)), item.Name);
                shortcuts = after;

                bool ok = outcome is WingetOutcome.Done or WingetOutcome.AlreadyInstalled or WingetOutcome.DoneAfterRestart;
                item.Status = ok ? Status.Ok : Status.Error;
                item.Detail = outcome switch
                {
                    WingetOutcome.Done => "Installé",
                    WingetOutcome.AlreadyInstalled => "Déjà installé",
                    WingetOutcome.DoneAfterRestart => "Installé, actif après un redémarrage du PC",
                    WingetOutcome.RestartThenRetry => "Redémarre le PC, puis relance l'installation.",
                    _ => Winget.Explain(code) ?? $"Échec (code 0x{code:X8}). Le détail technique est disponible en bas de l'écran.",
                };

                // On ne croit pas l'installeur sur parole : le logiciel est-il vraiment dans le dossier demandé ?
                if (ok && target is not null && outcome != WingetOutcome.AlreadyInstalled)
                {
                    if (FolderHasContent(target)) item.Detail += $" dans {target}";
                    else
                    {
                        item.Status = Status.Warning;
                        item.Detail = "Installé, mais à l'emplacement habituel : l'installeur n'a pas tenu compte du dossier choisi.";
                        misplaced++;
                    }
                }

                if (ok)
                {
                    succeeded++;
                    finished.Add(item);
                    if (outcome != WingetOutcome.AlreadyInstalled) item.LaunchPath = shortcut;
                    item.IsInstalled = true;
                    item.IsSelected = false;
                }
                else
                {
                    failures++;
                }
            }

            Report(100, "Terminé.");
            string done = Plural(succeeded, "logiciel installé", "logiciels installés");
            Headline = failures == 0 ? $"Terminé : {done}" : $"Terminé : {done}, {failures} en échec";
            SubText = failures == 0 && misplaced > 0 ? "Tout est installé, mais pas toujours dans le dossier demandé : regarde les lignes en orange."
                : failures == 0 ? "Tout s'est bien passé. Tu peux fermer l'appli ou retourner à la liste."
                : "L'explication est sous le nom de chaque logiciel en échec. Tu peux réessayer ceux-là.";
            MarkDone(allGood: failures == 0 && misplaced == 0);
            Logger.Info($"Installation : {succeeded} réussi(s), {failures} en échec");
        }
        catch (OperationCanceledException)
        {
            foreach (var item in items.Where(i => i.Status is Status.Working or Status.None))
            {
                item.Detail = item.Status == Status.Working ? "Installation annulée" : "Pas installé";
                item.Status = Status.Warning;
            }
            Headline = "Installation annulée";
            SubText = "Ce qui était déjà installé le reste. Tu peux réessayer le reste.";
        }
        catch (Win32Exception ex)
        {
            Headline = "winget est introuvable";
            SubText = "L'outil de Windows qui installe les logiciels ne répond pas. Retourne à la liste et clique sur « Activer winget ».";
            Logger.Error("Installation", ex);
        }
        catch (Exception ex)
        {
            Headline = "L'installation a échoué";
            SubText = ex.Message.Trim();
            Logger.Error("Installation", ex);
        }
        finally
        {
            _notDone = [.. items.Where(i => !finished.Contains(i))];
            HasFailures = _notDone.Count > 0;
            EndOperation();
            UpdateSelection();
            UpdateInstalledLink();
        }
    }

    /// <summary>Le bouton rond : « Annuler » pendant l'installation, « Retour à la liste » ensuite.</summary>
    [RelayCommand]
    private void Ring()
    {
        if (IsBusy) Cancel();
        else BackToCatalogue();
    }

    [RelayCommand]
    private void BackToCatalogue()
    {
        if (IsBusy) return;
        Step = Step.Catalogue;
        RebuildGroups();
    }

    [RelayCommand]
    private void ToggleLog() => ShowLog = !ShowLog;

    /// <summary>Bouton « Ouvrir » à côté d'un logiciel qui vient d'être installé.</summary>
    [RelayCommand]
    private void Launch(AppItem? app)
    {
        if (app?.LaunchPath is { } shortcut) Shortcuts.Open(shortcut);
    }

    /// <summary>Copie le résultat de l'installation, ligne par ligne, pour le coller à quelqu'un qui peut aider.</summary>
    [RelayCommand]
    private void CopySummary()
    {
        Dialogs.CopyText?.Invoke(BuildSummary());
        CopyLabel = "Résumé copié";
    }

    public string BuildSummary()
    {
        var lines = new List<string> { $"Clean Slate {typeof(CatalogueViewModel).Assembly.GetName().Version?.ToString(3)} : {Headline}" };
        lines.AddRange(Queue.Select(a => $"- {a.Name} ({a.WingetId}) : {a.Detail}"));
        return string.Join(Environment.NewLine, lines);
    }

    private static bool FolderHasContent(string folder)
    {
        try { return Directory.Exists(folder) && Directory.EnumerateFileSystemEntries(folder).Any(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return false; }
    }

    private static string Plural(int count, string singular, string plural) => $"{count} {(count > 1 ? plural : singular)}";
}
