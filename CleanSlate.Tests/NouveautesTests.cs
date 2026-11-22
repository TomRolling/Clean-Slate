using System.IO;
using CleanSlate.Models;
using CleanSlate.Services;
using CleanSlate.ViewModels;

namespace CleanSlate.Tests;

/// <summary>Packs, fichier de sélection, applis du Store, étiquettes « payant / compte », bouton « Ouvrir ».</summary>
public sealed class NouveautesTests
{
    private static readonly List<AppItem> Apps = [.. Catalog.Create().SelectMany(c => c.Apps)];

    [Fact]
    public void Chaque_pack_ne_contient_que_des_logiciels_du_catalogue()
    {
        var ids = Apps.Select(a => a.WingetId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var pack in Catalog.Packs)
        {
            Assert.NotEmpty(pack.Ids);
            Assert.All(pack.Ids, id => Assert.True(ids.Contains(id), $"Pack « {pack.Name} » : {id} n'est pas dans le catalogue."));
            Assert.Equal(pack.Ids.Count, pack.Ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }
    }

    [Fact]
    public void Un_clic_sur_un_pack_coche_tout_le_lot_et_un_second_le_decoche()
    {
        var viewModel = new CatalogueViewModel();
        var pack = viewModel.Packs.Single(p => p.Name == "Gamer");
        Assert.False(pack.IsActive);

        viewModel.TogglePackCommand.Execute(pack);
        Assert.All(pack.Apps, a => Assert.True(a.IsSelected));
        Assert.True(pack.IsActive);

        viewModel.TogglePackCommand.Execute(pack);
        Assert.All(pack.Apps, a => Assert.False(a.IsSelected));
        Assert.False(pack.IsActive);
    }

    [Fact]
    public void Les_etiquettes_payant_et_compte_ne_citent_que_des_logiciels_du_catalogue()
    {
        var ids = Apps.Select(a => a.WingetId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var labelled = CatalogAccess.Paid.Concat(CatalogAccess.Trial).Concat(CatalogAccess.Account).ToList();
        Assert.All(labelled, id => Assert.True(ids.Contains(id), $"{id} n'est pas dans le catalogue."));
        Assert.Equal(labelled.Count, labelled.Distinct(StringComparer.OrdinalIgnoreCase).Count());   // une seule étiquette par logiciel

        Assert.Equal(Access.Paid, Apps.Single(a => a.Name == "NordVPN").Access);
        Assert.Equal(Access.Account, Apps.Single(a => a.Name == "Spotify").Access);
        Assert.Equal(Access.Free, Apps.Single(a => a.Name == "VLC").Access);
    }

    [Fact]
    public void Les_applis_du_Store_ont_un_nom_de_paquet_et_ne_proposent_pas_de_dossier()
    {
        var store = Apps.Where(a => a.IsStoreApp).ToList();
        Assert.NotEmpty(store);
        foreach (var app in store)
        {
            Assert.Matches("^[A-Z0-9]{12}$", app.WingetId);                       // identifiant du Microsoft Store
            Assert.Matches(@"^[A-Za-z0-9.]+_[a-z0-9]{13}$", app.StorePackage!);   // nom de paquet Windows
            Assert.False(app.CanChooseLocation);
        }
    }

    [Fact]
    public void Une_selection_enregistree_se_recharge_avec_ses_dossiers()
    {
        string file = Path.Combine(Path.GetTempPath(), $"installetout-test-{Guid.NewGuid():N}.json");
        try
        {
            var source = new CatalogueViewModel();
            var all = source.Filters[1];
            source.CurrentFilter = all;
            var apps = source.Groups.SelectMany(g => g.Apps).ToList();
            var steam = apps.Single(a => a.WingetId == "Valve.Steam");
            steam.IsSelected = true;
            steam.InstallFolder = @"D:\Jeux";
            apps.Single(a => a.WingetId == "Google.Chrome").IsSelected = true;
            SelectionFile.Save(file, apps.Where(a => a.IsSelected));

            var target = new CatalogueViewModel();
            target.LoadSelection(file);
            var loaded = target.Groups.SelectMany(g => g.Apps).ToList();   // « Ma sélection » s'est ouverte

            Assert.Equal(["Google.Chrome", "Valve.Steam"], loaded.Select(a => a.WingetId).Order());
            Assert.Equal(@"D:\Jeux", loaded.Single(a => a.WingetId == "Valve.Steam").InstallFolder);
            Assert.Contains("2 logiciels cochés", target.PageHint);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void Un_fichier_de_selection_ne_peut_pas_glisser_d_option_dans_la_commande()
    {
        string file = Path.Combine(Path.GetTempPath(), $"installetout-test-{Guid.NewGuid():N}.json");
        try
        {
            // Un dossier avec un guillemet (pour ajouter des options à winget), un chemin relatif, un logiciel inconnu.
            File.WriteAllText(file, """
                { "version": 1, "logiciels": [
                    { "id": "Valve.Steam", "dossier": "D:\\Jeux\" --override \"/x" },
                    { "id": "VideoLAN.VLC", "dossier": "..\\ailleurs" },
                    { "id": "Inconnu.Logiciel" } ] }
                """);
            var viewModel = new CatalogueViewModel();
            viewModel.LoadSelection(file);
            var loaded = viewModel.Groups.SelectMany(g => g.Apps).ToList();

            Assert.Equal(2, loaded.Count);
            Assert.All(loaded, a => Assert.Null(a.InstallFolder));
            Assert.Contains("1 absent", viewModel.PageHint);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task Winget_refuse_un_dossier_contenant_un_guillemet()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Winget.InstallAsync("Valve.Steam", false, "Steam", "D:\\Jeux\" --override \"x", null, CancellationToken.None));
    }

    [Fact]
    public void Un_fichier_quelconque_n_est_pas_pris_pour_une_selection()
    {
        string file = Path.Combine(Path.GetTempPath(), $"installetout-test-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(file, "ceci n'est pas du JSON");
            Assert.Throws<InvalidDataException>(() => SelectionFile.Load(file));
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void Le_bouton_Ouvrir_prend_le_raccourci_du_logiciel_pas_celui_du_desinstalleur()
    {
        string[] created =
        [
            @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Krita\Uninstall Krita.lnk",
            @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Krita\Krita (safe mode).lnk",
            @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Krita\Krita.lnk",
        ];
        Assert.Equal(created[2], Shortcuts.Choose(created, "Krita"));
        Assert.Null(Shortcuts.Choose([created[0]], "Krita"));
        Assert.Null(Shortcuts.Choose([], "Krita"));
    }

    [Fact]
    public void L_ecran_Ou_installer_indique_le_disque_vise()
    {
        var viewModel = new CatalogueViewModel();
        viewModel.CurrentFilter = viewModel.Filters[1];
        viewModel.Groups.SelectMany(g => g.Apps).Single(a => a.WingetId == "Valve.Steam").IsSelected = true;

        viewModel.InstallCommand.Execute(null);

        string letter = Disks.SystemDrive.TrimEnd('\\');
        Assert.StartsWith(letter, viewModel.Choosable.Single().DriveText);
        Assert.Contains(letter, viewModel.DiskText);
    }
}
