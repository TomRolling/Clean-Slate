using CleanSlate.Models;
using CleanSlate.Services;

namespace CleanSlate.Tests;

/// <summary>La vérification « déjà installé » faite sur le PC (registre et paquets Windows), en plus de winget.</summary>
public sealed class InstalledAppsTests
{
    private static readonly List<AppItem> Apps = [.. Catalog.Create().SelectMany(c => c.Apps)];
    private static AppItem App(string id) => Apps.Single(a => a.WingetId == id);

    [Theory]
    [InlineData("WinRAR 7.23 (64-bit)", "WinRAR")]
    [InlineData("Stremio version 5.0.26", "Stremio")]
    [InlineData("CurseForge 1.315.1-37042", "CurseForge")]
    [InlineData("PuTTY release 0.85 (64-bit)", "PuTTY")]
    [InlineData("7-Zip 24.08 (x64)", "7-Zip")]
    [InlineData("Microsoft Visual Studio Code (User)", "Microsoft Visual Studio Code")]
    [InlineData("HWiNFO® 64", "HWiNFO 64")]
    [InlineData("Brave", "Brave")]
    public void Le_nom_est_debarrasse_de_sa_version(string affiche, string attendu) =>
        Assert.Equal(attendu, InstalledApps.Simplify(affiche));

    [Fact]
    public void Les_logiciels_que_winget_ne_reconnait_pas_sont_quand_meme_vus()
    {
        // Noms relevés tels quels sur le PC de Tom.
        var pc = new InstalledApps(
            ["Brave", "WinRAR 7.23 (64-bit)", "Microsoft Visual Studio Code (User)", "VLC media player", "Steam"],
            ["SpotifyAB.SpotifyMusic_zpdnekdrzrea0", "Claude_pzs8sxrjxfjjc", "Microsoft.Todos_8wekyb3d8bbwe"]);

        Assert.True(pc.Contains(App("Brave.Brave")));
        Assert.True(pc.Contains(App("RARLab.WinRAR")));
        Assert.True(pc.Contains(App("Microsoft.VisualStudioCode")));
        Assert.True(pc.Contains(App("VideoLAN.VLC")));
        Assert.True(pc.Contains(App("Spotify.Spotify")));       // version du Microsoft Store
        Assert.True(pc.Contains(App("Anthropic.Claude")));
        Assert.True(pc.Contains(App("9NBLGGH5R558")));          // Microsoft To Do (appli du Store)
        Assert.False(pc.Contains(App("Mozilla.Firefox")));
    }

    [Fact]
    public void Un_nom_qui_ressemble_ne_suffit_pas()
    {
        var pc = new InstalledApps(
            ["Opera GX Stable 120.0.5543.85", "Python 3.14.10 (64-bit)", "Steamworks Common Redistributables", "Plex Media Server"],
            []);

        Assert.True(pc.Contains(App("Opera.OperaGX")));
        Assert.False(pc.Contains(App("Opera.Opera")));           // Opera GX n'est pas Opera
        Assert.False(pc.Contains(App("Python.Python.3.13")));    // une autre version de Python
        Assert.False(pc.Contains(App("Valve.Steam")));
        Assert.False(pc.Contains(App("Plex.Plex")));
    }

    [Fact]
    public void La_famille_des_IA_existe_et_contient_les_assistants_connus()
    {
        var ia = Catalog.Create().Single(c => c.Name == "Intelligence artificielle");
        var ids = ia.Apps.Select(a => a.WingetId).ToList();
        Assert.Contains("OpenAI.ChatGPT", ids);
        Assert.Contains("Anthropic.Claude", ids);
        Assert.Contains("Google.GeminiDesktop", ids);
    }
}
