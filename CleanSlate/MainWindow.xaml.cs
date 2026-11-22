using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using CleanSlate.Services;
using CleanSlate.ViewModels;

namespace CleanSlate;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var viewModel = new MainViewModel();
        DataContext = viewModel;

        // Une fois la fenêtre affichée : ménage d'une éventuelle mise à jour précédente, puis vérification de version.
        Loaded += async (_, _) =>
        {
            UpdateService.CleanUpAfterUpdate();
            // Deux questions posées à l'ouverture, rien d'autre : « qu'est-ce qui est déjà installé ? » (à winget)
            // et « y a-t-il une version plus récente de l'appli ? » (à GitHub).
            await Task.WhenAll(viewModel.Catalogue.InitializeAsync(), viewModel.CheckForUpdateAsync());

            // Un fichier de sélection glissé sur l'exécutable (ou donné en argument) est ouvert directement.
            // On attend la réponse de winget pour ne pas recocher ce qui est déjà installé.
            string? file = Environment.GetCommandLineArgs().Skip(1).FirstOrDefault(System.IO.File.Exists);
            if (file is not null) viewModel.Catalogue.LoadSelection(file);
        };
    }

    /// <summary>Appelé quand la fenêtre Windows existe réellement : on peut alors assombrir sa barre de titre.</summary>
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var handle = new WindowInteropHelper(this).Handle;
        var source = HwndSource.FromHwnd(handle);
        source?.AddHook(BlockResizeBorders);
        source?.AddHook(HoldRestartDuringInstall);
        RestartService.Attach(handle, AskToRestart);
        Attention.Attach(handle);
        Dialogs.Show = Confirm;
        Dialogs.PickFolder = PickFolder;
        Dialogs.PickSaveFile = PickSaveFile;
        Dialogs.PickOpenFile = PickOpenFile;
        Dialogs.CopyText = CopyText;
        int enabled = 1;
        // Windows 11 et Windows 10 récent utilisent l'attribut 20 ; les Windows 10 d'avant 2020 utilisaient le 19.
        // Si les deux échouent (Windows encore plus ancien), la barre de titre reste claire : sans gravité.
        if (DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref enabled, sizeof(int)) != 0)
            _ = DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref enabled, sizeof(int));
    }

    // Taille pour laquelle l'interface a été dessinée (zone utile de la fenêtre par défaut).
    private const double DesignWidth = 1024, DesignHeight = 641;

    // En plein écran on ne grossit que d'une partie de la place gagnée (40 %), et jamais au-delà de 1,5 fois :
    // l'interface reste confortable sans donner l'impression d'être zoomée.
    private const double Damping = 0.4, MinScale = 0.85, MaxScale = 1.5;

    /// <summary>
    /// Il n'y a que deux tailles : la fenêtre de base et le plein écran. Quand la taille change, on calcule un
    /// facteur d'échelle unique à partir de la plus petite des deux proportions (largeur, hauteur). Le
    /// « LayoutTransform » agrandit alors textes, boutons et marges ensemble, et WPF donne la place restante aux listes.
    /// </summary>
    private void OnHostSizeChanged(object sender, SizeChangedEventArgs e)
    {
        double ratio = Math.Min(e.NewSize.Width / DesignWidth, e.NewSize.Height / DesignHeight);
        double scale = ratio > 1 ? 1 + (ratio - 1) * Damping : ratio;
        scale = Math.Clamp(scale, MinScale, MaxScale);
        RootScale.ScaleX = scale;
        RootScale.ScaleY = scale;
    }

    /// <summary>
    /// Windows demande en permanence « sur quoi se trouve la souris ? ». Quand il répond « un bord qui redimensionne »,
    /// on le remplace par « un bord simple » : la fenêtre ne peut plus être étirée à la souris, mais le bouton
    /// Agrandir et le double-clic sur la barre de titre fonctionnent toujours.
    /// </summary>
    private static IntPtr BlockResizeBorders(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message != WM_NCHITTEST) return IntPtr.Zero;

        long hit = DefWindowProc(hwnd, message, wParam, lParam).ToInt64();
        if (hit is >= HTLEFT and <= HTBOTTOMRIGHT)
        {
            handled = true;
            return HTBORDER;
        }
        return IntPtr.Zero;
    }

    /// <summary>
    /// Avant d'arrêter ou de redémarrer le PC, Windows demande à chaque fenêtre si elle est d'accord.
    /// Pendant une installation on répond non : un installeur qui redémarre le PC de lui-même est retenu,
    /// et la question sera posée à l'utilisateur à la fin de l'opération.
    /// </summary>
    private static IntPtr HoldRestartDuringInstall(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message != WM_QUERYENDSESSION || !RestartService.TryHoldRestart()) return IntPtr.Zero;

        handled = true;
        return IntPtr.Zero;   // 0 = « non, pas maintenant »
    }

    /// <summary>Affiche la question « Redémarrer maintenant / Plus tard ». Rien ne redémarre sans le clic.</summary>
    private void AskToRestart(string reason)
    {
        // BeginInvoke : la question s'affiche une fois que l'opération en cours a fini de mettre l'écran à jour.
        Dispatcher.BeginInvoke(() =>
        {
            if (!IsLoaded) return;
            var request = new ConfirmRequest("", "Redémarrage nécessaire", reason,
                "Enregistre ton travail avant de redémarrer. Si tu choisis « Plus tard », rien ne se passe : tu redémarreras quand tu voudras.",
                ConfirmLabel: "Redémarrer maintenant", CancelLabel: "Plus tard");
            if (Confirm(request)) RestartService.RestartNow();
        });
    }

    /// <summary>Affiche une fenêtre de confirmation par-dessus l'appli. Vrai seulement si l'utilisateur a confirmé.</summary>
    private bool Confirm(ConfirmRequest request) =>
        IsLoaded && new Views.ConfirmDialog(request) { Owner = this }.ShowDialog() == true;

    /// <summary>Le sélecteur de dossier standard de Windows.</summary>
    private string? PickFolder(string title)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = title };
        return dialog.ShowDialog(this) == true ? dialog.FolderName : null;
    }

    private string? PickSaveFile(string fileName, string filter)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog { FileName = fileName, Filter = filter, AddExtension = true };
        return dialog.ShowDialog(this) == true ? dialog.FileName : null;
    }

    private string? PickOpenFile(string filter)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = filter };
        return dialog.ShowDialog(this) == true ? dialog.FileName : null;
    }

    private static void CopyText(string text)
    {
        // Le presse-papiers peut être occupé un instant par un autre programme : dans ce cas on n'insiste pas.
        try { Clipboard.SetDataObject(text, copy: true); }
        catch (COMException) { }
    }

    // Messages et valeurs documentés par Microsoft (winuser.h).
    private const int WM_QUERYENDSESSION = 0x0011;
    private const int WM_NCHITTEST = 0x0084;
    private const long HTLEFT = 10, HTBOTTOMRIGHT = 17;
    private const int HTBORDER = 18;

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);

    /// <summary>Fermer la fenêtre = quitter complètement.</summary>
    protected override void OnClosed(EventArgs e)
    {
        AppLifetime.Shutdown();          // annule les tâches en cours et tue les processus enfants
        base.OnClosed(e);
        Application.Current.Shutdown();  // arrête l'application elle-même
    }

    // API Windows documentée (dwmapi.h) : attribut 20 = barre de titre sombre.
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
