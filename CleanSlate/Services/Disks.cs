using System.IO;

namespace CleanSlate.Services;

/// <summary>La place libre sur les disques, pour l'écran « Où installer ? ».</summary>
public static class Disks
{
    /// <summary>En dessous de 10 Go libres, on prévient : quelques gros logiciels suffisent à remplir le disque.</summary>
    public const long LowSpace = 10L * 1024 * 1024 * 1024;

    /// <summary>Le disque de Windows (« C:\ » presque toujours) : celui où vont les logiciels qui ne laissent pas le choix.</summary>
    public static string SystemDrive => Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\";

    /// <summary>Octets libres sur le disque qui contient ce chemin, ou null si on ne peut pas le savoir (disque réseau, lecteur absent).</summary>
    public static long? FreeBytes(string path)
    {
        try
        {
            string? root = Path.GetPathRoot(Path.GetFullPath(path));
            if (string.IsNullOrEmpty(root)) return null;
            var drive = new DriveInfo(root);
            return drive.IsReady ? drive.AvailableFreeSpace : null;
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>« D: · 310 Go libres », ou « D: » si la place est inconnue.</summary>
    public static (string Text, bool IsLow) Describe(string path)
    {
        string letter = (Path.GetPathRoot(path) ?? path).TrimEnd('\\');
        return FreeBytes(path) is { } free
            ? ($"{letter} · {Format.Size(free)} libres", free < LowSpace)
            : (letter, false);
    }
}
