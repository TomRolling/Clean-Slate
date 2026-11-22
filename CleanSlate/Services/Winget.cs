using System.ComponentModel;
using System.IO;
using System.Text.Json;

namespace CleanSlate.Services;

/// <summary>Ce que winget a répondu, une fois son code de sortie traduit.</summary>
public enum WingetOutcome
{
    /// <summary>Installé.</summary>
    Done,
    /// <summary>Rien à faire : le logiciel était déjà là.</summary>
    AlreadyInstalled,
    /// <summary>Réussi, mais actif seulement après un redémarrage du PC.</summary>
    DoneAfterRestart,
    /// <summary>Échec : il faut redémarrer le PC puis recommencer.</summary>
    RestartThenRetry,
    Failed,
}

/// <summary>
/// Le seul endroit qui parle à winget : mêmes options partout, redémarrages retenus pendant l'installation,
/// et codes de sortie traduits une seule fois.
/// </summary>
public static class Winget
{
    /// <summary>
    /// Vrai pour la ligne où winget annonce un téléchargement (« Downloading https://… »). Lancé par un programme,
    /// winget n'écrit pas sa barre d'avancement : cette ligne et la suivante sont tout ce qu'on sait. On reconnaît
    /// l'adresse plutôt que le mot, parce que winget parle la langue de Windows.
    /// </summary>
    public static bool IsDownloadLine(string line) =>
        !line.StartsWith('>') && (line.Contains("https://") || line.Contains("http://"));

    // Codes de sortie documentés par Microsoft (winget « returnCodes »).
    private const int UpdateNotApplicable = unchecked((int)0x8A15002B);     // « No applicable update found »
    private const int PackageAlreadyInstalled = unchecked((int)0x8A150061);
    private const int AnotherVersionInstalled = unchecked((int)0x8A15010D);
    private const int HigherVersionInstalled = unchecked((int)0x8A15010E);
    private const int RebootRequiredToFinish = unchecked((int)0x8A150109);  // « Restart your PC to finish installation. »
    private const int RebootRequiredForInstall = unchecked((int)0x8A15010A);// « Installation failed. Restart your PC then try again. »
    private const int RebootInitiated = unchecked((int)0x8A15010B);         // « Your PC will restart to finish installation. »

    public static WingetOutcome Interpret(int exitCode) => exitCode switch
    {
        0 => WingetOutcome.Done,
        UpdateNotApplicable or PackageAlreadyInstalled or AnotherVersionInstalled or HigherVersionInstalled => WingetOutcome.AlreadyInstalled,
        RebootRequiredToFinish or RebootInitiated => WingetOutcome.DoneAfterRestart,
        RebootRequiredForInstall => WingetOutcome.RestartThenRetry,
        _ => WingetOutcome.Failed,
    };

    // Échecs que winget explique lui-même (même documentation).
    private const int DownloadFailed = unchecked((int)0x8A150008);
    private const int NoApplicableInstaller = unchecked((int)0x8A150010);
    private const int InstallerHashMismatch = unchecked((int)0x8A150011);   // « The installer file's hash does not match the manifest »
    private const int PackageNotFound = unchecked((int)0x8A150014);
    private const int BlockedByPolicy = unchecked((int)0x8A15003A);
    private const int ProhibitsElevation = unchecked((int)0x8A150056);      // « The installer cannot be run from an administrator context. »
    private const int PackageInUse = unchecked((int)0x8A150101);
    private const int InstallInProgress = unchecked((int)0x8A150102);
    private const int FileInUse = unchecked((int)0x8A150103);
    private const int MissingDependency = unchecked((int)0x8A150104);
    private const int DiskFull = unchecked((int)0x8A150105);
    private const int NoNetwork = unchecked((int)0x8A150107);
    private const int CancelledByUser = unchecked((int)0x8A15010C);
    private const int InstallBlockedByPolicy = unchecked((int)0x8A15010F);
    private const int InUseByApplication = unchecked((int)0x8A150111);
    private const int SystemNotSupported = unchecked((int)0x8A150113);

    /// <summary>Explication en clair d'un échec connu, ou null si le code ne dit rien d'utile.</summary>
    public static string? Explain(int exitCode) => exitCode switch
    {
        InstallerHashMismatch =>
            "L'éditeur a changé son fichier d'installation et winget ne l'a pas encore vérifié : par sécurité, rien n'est installé. " +
            "Réessaie dans quelques jours.",
        PackageNotFound => "winget ne trouve plus ce logiciel dans son catalogue.",
        NoApplicableInstaller or SystemNotSupported => "Ce logiciel n'existe pas pour ce PC (version de Windows ou type de processeur).",
        ProhibitsElevation => "Ce logiciel refuse d'être installé en administrateur, et l'installation avec le compte normal n'a pas pu être lancée.",
        PackageInUse or FileInUse or InUseByApplication => "Le logiciel est ouvert : ferme-le, puis réessaie.",
        InstallInProgress => "Une autre installation est déjà en cours sur le PC. Réessaie dans un instant.",
        MissingDependency => "Il manque un composant dont ce logiciel a besoin.",
        DiskFull => "Il n'y a plus assez de place sur le disque.",
        DownloadFailed or NoNetwork => "Le téléchargement a échoué : vérifie la connexion Internet, puis réessaie.",
        CancelledByUser => "L'installation a été refusée ou annulée dans la fenêtre de l'installeur.",
        BlockedByPolicy or InstallBlockedByPolicy => "L'installation est bloquée par les règles de l'organisation qui gère ce PC.",
        _ => null,
    };

    /// <summary>Renvoie la version de winget, ou null s'il n'est pas installé.</summary>
    public static async Task<string?> GetVersionAsync(CancellationToken token)
    {
        try
        {
            string? version = null;
            var output = new DirectProgress<string>(line => version ??= line.Trim());
            int code = await ProcessRunner.RunAsync("winget", "--version", output, token);
            return code == 0 ? version ?? "installé" : null;
        }
        catch (Win32Exception)
        {
            return null;   // winget.exe introuvable
        }
    }

    /// <summary>
    /// Identifiants winget des logiciels déjà présents sur le PC. On demande à winget d'exporter sa liste dans un
    /// fichier JSON (« winget export ») : pas de tableau à déchiffrer, donc pas d'erreur de lecture.
    /// Seuls les logiciels que winget sait relier à son catalogue y figurent.
    /// </summary>
    public static async Task<HashSet<string>> ListInstalledAsync(CancellationToken token)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Directory.CreateDirectory(AppInfo.LocalFolder);
        string file = Path.Combine(AppInfo.LocalFolder, "installes.json");
        try
        {
            File.Delete(file);
            // Options vérifiées avec « winget export --help ».
            await ProcessRunner.RunAsync("winget",
                $"export --output \"{file}\" --source winget --accept-source-agreements --disable-interactivity", null, token);
            if (!File.Exists(file)) return ids;

            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(file, token));
            foreach (var source in document.RootElement.GetProperty("Sources").EnumerateArray())
            {
                foreach (var package in source.GetProperty("Packages").EnumerateArray())
                {
                    if (package.GetProperty("PackageIdentifier").GetString() is { } id) ids.Add(id);
                }
            }
        }
        catch (Exception ex) when (ex is Win32Exception or IOException or JsonException or KeyNotFoundException or UnauthorizedAccessException)
        {
            Logger.Error("Lecture des logiciels déjà installés", ex);
        }
        finally
        {
            try { File.Delete(file); }
            catch { /* sans gravité */ }
        }
        return ids;
    }

    /// <summary>
    /// Demande à winget de rafraîchir son catalogue (« winget source update »), pour installer la toute dernière
    /// version publiée de chaque logiciel. Sans conséquence si cela échoue : winget garde le catalogue qu'il a déjà.
    /// </summary>
    public static async Task RefreshCatalogAsync(IProgress<string>? output, CancellationToken token)
    {
        try
        {
            output?.Report("> winget source update");
            int code = await ProcessRunner.RunAsync("winget", "source update --disable-interactivity", output, token);
            Logger.Info($"winget source update : code 0x{code:X8}");
        }
        catch (Win32Exception)
        {
            // winget introuvable : l'installation qui suit le dira.
        }
    }

    /// <summary>
    /// Installe un logiciel, dans sa dernière version : on ne donne jamais « --version », winget prend donc toujours
    /// la plus récente de son catalogue.
    /// </summary>
    /// <param name="fromStore">Vrai pour une appli du Microsoft Store (source « msstore » de winget).</param>
    /// <param name="displayName">Nom montré à l'utilisateur si un redémarrage est nécessaire.</param>
    /// <param name="location">Dossier d'installation voulu, ou null pour laisser l'installeur choisir.</param>
    /// <exception cref="Win32Exception">Si winget est introuvable.</exception>
    public static async Task<(WingetOutcome Outcome, int Code)> InstallAsync(
        string id, bool fromStore, string displayName, string? location, IProgress<string>? output, CancellationToken token)
    {
        // Le dossier est placé entre guillemets dans la commande : un guillemet dedans permettrait d'y glisser
        // d'autres options (le dossier peut venir d'un fichier de sélection reçu de quelqu'un d'autre).
        if (location?.Contains('"') == true) throw new ArgumentException("Dossier d'installation invalide.", nameof(location));

        // « --location » : dossier d'installation demandé (option vérifiée avec « winget install --help »).
        string where = location is null ? "" : $"--location \"{location.TrimEnd('\\')}\" ";

        // Jamais « --allow-reboot » : l'appli ne redémarre le PC que sur un clic de l'utilisateur.
        string arguments =
            $"install --id {id} --exact --source {(fromStore ? "msstore" : "winget")} --silent {where}" +
            "--accept-package-agreements --accept-source-agreements --disable-interactivity";

        output?.Report($"> winget {arguments}");
        Logger.Info($"winget install {id}");

        int code;
        using (RestartService.Guard())
        {
            code = await ProcessRunner.RunAsync("winget", arguments, output, token);

            // Quelques installeurs (Spotify…) refusent les droits d'administrateur : on recommence avec le compte normal.
            if (code == ProhibitsElevation)
            {
                output?.Report("Ce logiciel s'installe sans droits d'administrateur : nouvelle tentative avec le compte normal…");
                Logger.Info($"winget install {id} : relance sans droits d'administrateur");
                code = await StandardUser.RunWingetAsync(arguments, output, token) ?? code;
            }
        }

        Logger.Info($"winget install {id} : code 0x{code:X8}");

        var outcome = Interpret(code);
        if (outcome is WingetOutcome.DoneAfterRestart or WingetOutcome.RestartThenRetry)
            RestartService.Need($"« {displayName} » a besoin d'un redémarrage du PC pour finir de s'installer.");
        return (outcome, code);
    }
}
