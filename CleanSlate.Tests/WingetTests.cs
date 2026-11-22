using CleanSlate.Services;

namespace CleanSlate.Tests;

/// <summary>Vérifie la traduction des codes de sortie de winget, en particulier ceux qui parlent de redémarrage.</summary>
public sealed class WingetTests
{
    [Theory]
    [InlineData(0u, WingetOutcome.Done)]
    [InlineData(0x8A15002B, WingetOutcome.AlreadyInstalled)]   // déjà là, rien de plus récent
    [InlineData(0x8A150061, WingetOutcome.AlreadyInstalled)]   // déjà installé
    [InlineData(0x8A150109, WingetOutcome.DoneAfterRestart)]   // redémarrer pour terminer l'installation
    [InlineData(0x8A15010B, WingetOutcome.DoneAfterRestart)]   // l'installeur veut redémarrer
    [InlineData(0x8A15010A, WingetOutcome.RestartThenRetry)]   // redémarrer puis réessayer : ce n'est pas une réussite
    [InlineData(0x8A150056, WingetOutcome.Failed)]             // refuse les droits d'administrateur
    [InlineData(1u, WingetOutcome.Failed)]
    public void Chaque_code_de_winget_est_bien_traduit(uint code, WingetOutcome expected)
    {
        Assert.Equal(expected, Winget.Interpret(unchecked((int)code)));
    }

    [Fact]
    public void Un_logiciel_ouvert_est_explique()
    {
        Assert.Contains("ferme-le", Winget.Explain(unchecked((int)0x8A150101)));
    }

    [Fact]
    public void Un_code_inconnu_n_a_pas_d_explication_inventee()
    {
        Assert.Null(Winget.Explain(1));
    }
}
