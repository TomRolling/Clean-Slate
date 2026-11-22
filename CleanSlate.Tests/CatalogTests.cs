using CleanSlate.Services;
using CleanSlate.ViewModels;

namespace CleanSlate.Tests;

/// <summary>Garde-fous sur le catalogue : une faute de frappe dans la liste se voit ici, pas chez l'utilisateur.</summary>
public sealed class CatalogTests
{
    private static readonly List<Models.AppItem> Apps = [.. Catalog.Create().SelectMany(c => c.Apps)];

    [Fact]
    public void Aucun_logiciel_n_est_en_double()
    {
        var doubles = Apps.GroupBy(a => a.WingetId, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key);
        Assert.Empty(doubles);
    }

    [Fact]
    public void Chaque_logiciel_a_un_nom_une_description_et_un_identifiant_propre()
    {
        foreach (var app in Apps)
        {
            Assert.False(string.IsNullOrWhiteSpace(app.Name));
            Assert.False(string.IsNullOrWhiteSpace(app.Description), $"{app.Name} n'a pas de description.");
            // Un identifiant winget ne contient ni espace ni guillemet : il est placé tel quel dans la commande.
            // (Les applis du Microsoft Store ont un identifiant d'une autre forme, vérifié dans NouveautesTests.)
            if (!app.IsStoreApp) Assert.Matches(@"^[A-Za-z0-9][A-Za-z0-9.+_-]*\.[A-Za-z0-9.+_-]+$", app.WingetId);
        }
    }

    [Fact]
    public void Chaque_famille_a_une_icone_et_au_moins_un_logiciel()
    {
        foreach (var category in Catalog.Create())
        {
            Assert.NotEmpty(category.Apps);
            // Les icônes sont des caractères de la police d'icônes Windows (zone privée Unicode).
            Assert.True(category.Icon.Length == 1 && category.Icon[0] >= '\uE000' && category.Icon[0] <= '\uF8FF',
                $"La famille « {category.Name} » n'a pas d'icône.");
        }
    }

    [Fact]
    public void Les_composants_systeme_ne_proposent_jamais_de_choisir_un_dossier()
    {
        var components = Catalog.Create().Single(c => c.Name == Catalog.ComponentsCategory);
        Assert.DoesNotContain(components.Apps, a => a.CanChooseLocation);
    }

    [Fact]
    public void Le_dossier_choisi_recoit_un_sous_dossier_au_nom_du_logiciel()
    {
        var steam = Apps.Single(a => a.WingetId == "Valve.Steam");
        Assert.Null(steam.TargetFolder);

        steam.InstallFolder = @"D:\Jeux";
        Assert.Equal(@"D:\Jeux\Steam", steam.TargetFolder);
    }

    [Fact]
    public void La_recherche_ignore_les_accents_et_les_majuscules()
    {
        var viewModel = new CatalogueViewModel { SearchText = "EDITEUR" };
        Assert.Contains(viewModel.Groups.SelectMany(g => g.Apps), a => a.Name == "Notepad++");
    }

    [Fact]
    public void Cocher_puis_Suivant_separe_les_logiciels_a_dossier_reglable_des_autres()
    {
        var viewModel = new CatalogueViewModel();
        var all = viewModel.Filters[1];
        viewModel.CurrentFilter = all;
        var apps = viewModel.Groups.SelectMany(g => g.Apps).ToList();
        apps.Single(a => a.WingetId == "Valve.Steam").IsSelected = true;
        apps.Single(a => a.WingetId == "Google.Chrome").IsSelected = true;
        apps.Single(a => a.WingetId == "Microsoft.DirectX").IsSelected = true;

        viewModel.InstallCommand.Execute(null);

        Assert.Equal(Step.Review, viewModel.Step);
        Assert.Equal("Microsoft.DirectX", viewModel.Queue[0].WingetId);   // les composants passent en premier
        Assert.Equal(["Valve.Steam"], viewModel.Choosable.Select(a => a.WingetId));
        Assert.Contains("Google Chrome", viewModel.FixedText);
    }
}
