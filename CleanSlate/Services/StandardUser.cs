using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace CleanSlate.Services;

/// <summary>
/// Lance winget « sans les droits d'administrateur ». Quelques installeurs (Spotify par exemple) refusent d'être
/// lancés en administrateur, alors que l'appli tourne en administrateur. Méthode documentée par Microsoft
/// (Raymond Chen, « How can I launch an unelevated process from my elevated process ») : on emprunte le jeton
/// de sécurité du Bureau de Windows (explorer.exe), qui tourne toujours avec les droits normaux de la session.
/// </summary>
internal static class StandardUser
{
    /// <returns>Le code de sortie de winget, ou null s'il n'a pas pu être lancé de cette façon.</returns>
    public static async Task<int?> RunWingetAsync(string arguments, IProgress<string>? output, CancellationToken token)
    {
        // Un processus lancé ainsi ne peut pas nous envoyer sa sortie en direct : elle passe par un fichier,
        // dans un dossier où le compte normal a le droit d'écrire.
        string logFile;
        try
        {
            Directory.CreateDirectory(AppInfo.CommonFolder);
            logFile = Path.Combine(AppInfo.CommonFolder, $"winget-{Guid.NewGuid():N}.log");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        string commandLine = $"\"{ProcessRunner.SystemTool("cmd.exe")}\" /d /s /c \"winget {arguments} > \"{logFile}\" 2>&1\"";

        IntPtr process;
        int processId;
        try
        {
            (process, processId) = Start(commandLine);
        }
        catch (Win32Exception ex)
        {
            Logger.Error("Lancement de winget sans droits d'administrateur", ex);
            return null;
        }

        try
        {
            // Pour que la fermeture de l'appli ou le bouton Annuler arrêtent aussi ce winget-là.
            Process? handle = null;
            try
            {
                handle = Process.GetProcessById(processId);
                AppLifetime.RegisterChildProcess(handle);
            }
            catch (ArgumentException)
            {
                // Déjà terminé.
            }

            using var registration = token.Register(() =>
            {
                try { handle?.Kill(entireProcessTree: true); }
                catch { /* déjà terminé */ }
            });

            await Task.Run(() => WaitForSingleObject(process, Infinite), CancellationToken.None).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();

            if (!GetExitCodeProcess(process, out uint exitCode)) return null;

            try
            {
                foreach (string line in await File.ReadAllLinesAsync(logFile, CancellationToken.None).ConfigureAwait(false))
                {
                    if (line.Trim().Length > 1) output?.Report(line.TrimEnd());
                }
            }
            catch (IOException)
            {
                // Pas de sortie à montrer : le code de sortie suffit.
            }
            return unchecked((int)exitCode);
        }
        finally
        {
            CloseHandle(process);
            try { File.Delete(logFile); }
            catch { /* sans gravité : le fichier fait quelques Ko */ }
        }
    }

    /// <summary>
    /// Ouvre un fichier ou un raccourci avec le compte normal, comme un double-clic dans l'Explorateur.
    /// Sert au bouton « Ouvrir » : un logiciel qu'on vient d'installer ne doit pas démarrer en administrateur.
    /// </summary>
    /// <returns>Faux si ce lancement n'a pas été possible.</returns>
    public static bool TryOpen(string path)
    {
        try
        {
            // « start "" "chemin" » : la commande de Windows qui ouvre un fichier avec le programme prévu pour lui.
            var (process, _) = Start($"\"{ProcessRunner.SystemTool("cmd.exe")}\" /d /s /c \"start \"\" \"{path}\"\"");
            CloseHandle(process);
            return true;
        }
        catch (Win32Exception ex)
        {
            Logger.Error("Ouverture avec le compte normal", ex);
            return false;
        }
    }

    /// <summary>Démarre la commande avec le jeton du Bureau de Windows. Renvoie le handle du processus et son numéro.</summary>
    private static (IntPtr Process, int Id) Start(string commandLine)
    {
        IntPtr shellWindow = GetShellWindow();
        if (shellWindow == IntPtr.Zero) throw new Win32Exception("Le Bureau de Windows (explorer.exe) ne tourne pas.");
        _ = GetWindowThreadProcessId(shellWindow, out uint shellProcessId);

        IntPtr shellProcess = IntPtr.Zero, shellToken = IntPtr.Zero, primaryToken = IntPtr.Zero;
        try
        {
            shellProcess = OpenProcess(ProcessQueryInformation, false, shellProcessId);
            if (shellProcess == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());

            if (!OpenProcessToken(shellProcess, TokenDuplicate, out shellToken))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            if (!DuplicateTokenEx(shellToken, TokenRightsForNewProcess, IntPtr.Zero, SecurityImpersonation, TokenPrimary, out primaryToken))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            var startup = new STARTUPINFO { cb = Marshal.SizeOf<STARTUPINFO>() };
            if (!CreateProcessWithTokenW(primaryToken, LogonWithProfile, null, commandLine, CreateNoWindow,
                    IntPtr.Zero, null, ref startup, out var info))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            CloseHandle(info.hThread);
            return (info.hProcess, info.dwProcessId);
        }
        finally
        {
            if (primaryToken != IntPtr.Zero) CloseHandle(primaryToken);
            if (shellToken != IntPtr.Zero) CloseHandle(shellToken);
            if (shellProcess != IntPtr.Zero) CloseHandle(shellProcess);
        }
    }

    // Valeurs documentées par Microsoft (winnt.h, winbase.h).
    private const uint ProcessQueryInformation = 0x0400;
    private const uint TokenDuplicate = 0x0002;
    // TOKEN_QUERY | TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_ADJUST_DEFAULT | TOKEN_ADJUST_SESSIONID
    private const uint TokenRightsForNewProcess = 0x0008 | 0x0001 | 0x0002 | 0x0080 | 0x0100;
    private const int SecurityImpersonation = 2, TokenPrimary = 1;
    private const uint LogonWithProfile = 1;
    private const uint CreateNoWindow = 0x08000000;
    private const uint Infinite = 0xFFFFFFFF;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFO
    {
        public int cb;
        public string? lpReserved, lpDesktop, lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess, hThread;
        public int dwProcessId, dwThreadId;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint access, bool inheritHandle, uint processId);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool DuplicateTokenEx(IntPtr token, uint access, IntPtr attributes, int impersonationLevel, int tokenType, out IntPtr newToken);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateProcessWithTokenW(IntPtr token, uint logonFlags, string? applicationName, string commandLine,
        uint creationFlags, IntPtr environment, string? currentDirectory, ref STARTUPINFO startupInfo, out PROCESS_INFORMATION processInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetExitCodeProcess(IntPtr process, out uint exitCode);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr handle);
}
