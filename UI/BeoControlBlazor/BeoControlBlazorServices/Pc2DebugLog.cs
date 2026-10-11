using System.Text.RegularExpressions;

using BeoControl.Interfaces;

namespace BeoControlBlazorServices;

/// <summary>
/// Optional file log of the PC2's debug messages (USB traffic, mixer state, Masterlink
/// telegrams), for tracking down problems that don't show otherwise. On only when the
/// environment variable BEOCONTROL_PC2_LOG names a file. Keeps the newest ~5 MB: when the
/// file grows past that it becomes "&lt;file&gt;.1" and a new one starts.
/// </summary>
public static class Pc2DebugLog
{
    private const long MaxBytes = 5 * 1024 * 1024;
    private static readonly object Gate = new();
    private static readonly Regex Ansi = new(@"\x1b\[[0-9;]*m", RegexOptions.Compiled);

    /// <summary>The log file from BEOCONTROL_PC2_LOG, or null when logging is off.</summary>
    public static string? Path
    {
        get
        {
            var p = Environment.GetEnvironmentVariable("BEOCONTROL_PC2_LOG");
            return string.IsNullOrWhiteSpace(p) ? null : p;
        }
    }

    public static void Write(string path, LogMessage msg)
    {
        try
        {
            lock (Gate)
            {
                var dir = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                var info = new FileInfo(path);
                if (info.Exists && info.Length > MaxBytes)
                    File.Move(path, path + ".1", overwrite: true);
                File.AppendAllText(path,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{msg.Level}] {Ansi.Replace(msg.Text, "")}{Environment.NewLine}");
            }
        }
        catch (Exception)
        {
            // Never let debug logging disturb the device.
        }
    }
}
