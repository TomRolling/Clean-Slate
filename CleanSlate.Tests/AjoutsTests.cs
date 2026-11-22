using CleanSlate.Models;
using CleanSlate.Services;
using CleanSlate.ViewModels;

namespace CleanSlate.Tests;

/// <summary>Étape de téléchargement, « installer quand même », chemin raccourci par le milieu.</summary>
public sealed class AjoutsTests
{
    [Theory]
    [InlineData("Downloading https://github.com/audacity/audacity/releases/download/Audacity-4.0.2/audacity.msi", true)]
    [InlineData("Téléchargement en cours https://exemple.org/setup.exe", true)]
    [InlineData("Successfully verified installer hash", false)]
    [InlineData("> winget install --id X --source https://exemple.org", false)]
    public void La_ligne_de_telechargement_est_reconnue_dans_toutes_les_langues(string ligne, bool attendu) =>
        Assert.Equal(attendu, Winget.IsDownloadLine(ligne));

    [Fact]
    public void Un_logiciel_reconnu_sur_le_PC_peut_etre_coche_quand_meme()
    {
        var viewModel = new CatalogueViewModel();
        var steam = viewModel.Groups.SelectMany(g => g.Apps).Single(a => a.WingetId == "Valve.Steam");
        steam.IsInstalled = true;
        Assert.Equal("Déjà installé", steam.InstalledText);
        Assert.False(viewModel.InstallCommand.CanExecute(null) && viewModel.Queue.Count > 0);

        steam.IsSelected = true;
        Assert.Equal("Sera installé quand même", steam.InstalledText);
        Assert.Equal("1 logiciel coché", viewModel.SelectionText);
    }

    [Fact]
    public void Un_chemin_long_garde_son_debut_et_sa_fin()
    {
        string court = AppItem.ShortenMiddle(@"D:\Jeux\Lanceurs\Boutiques de jeux\Epic Games Launcher", 40);
        Assert.Equal(40, court.Length);
        Assert.StartsWith(@"D:\Jeux", court);
        Assert.EndsWith("Epic Games Launcher", court);
        Assert.Equal(@"D:\Jeux", AppItem.ShortenMiddle(@"D:\Jeux", 40));
    }
}
