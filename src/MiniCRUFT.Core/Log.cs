using System;
using System.IO;
using System.Text;

namespace MiniCRUFT.Core;

public static class Log
{
    private static readonly object Sync = new();
    private static StreamWriter? _writer;

    public static void Initialize(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _writer = new StreamWriter(path, append: false, Encoding.UTF8) { AutoFlush = true };
    }

    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message) => Write("ERROR", message);

    private static void Write(string level, string message)
    {
        var line = $"{DateTime.Now:HH:mm:ss} [{level}] {message}";
        lock (Sync)
        {
            Console.WriteLine(line);
            _writer?.WriteLine(line);
        }
    }
}
