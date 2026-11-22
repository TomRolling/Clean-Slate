using System.Windows;
using CleanSlate.Services;

namespace CleanSlate;

/// <summary>
/// Point d'entrée de l'application. Rien n'est lancé ici : pas de tâche de fond.
/// La fenêtre s'ouvre (StartupUri dans App.xaml) ; rien ne s'installe sans un clic.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {

        // Filet de sécurité : une erreur imprévue est notée dans le journal au lieu de fermer l'appli.
        DispatcherUnhandledException += (_, args) =>
        {
            Logger.Error("Erreur imprévue", args.Exception);
            args.Handled = true;
        };

        base.OnStartup(e);
    }
}
