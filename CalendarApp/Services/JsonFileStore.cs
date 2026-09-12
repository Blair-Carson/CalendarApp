using System;
using System.IO;
using System.Text.Json;

namespace CalendarApp.Services;

/// <summary>
/// Reads and writes a single JSON file under %AppData%\CalendarApp.
/// Writes go to a temporary file first so a crash mid-save cannot truncate good data,
/// and unreadable files are set aside rather than silently overwritten.
/// </summary>
public sealed class JsonFileStore<T>
    where T : class
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
    };

    private readonly string _path;

    public JsonFileStore(string fileName)
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CalendarApp");
        Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, fileName);
    }

    /// <summary>Returns the stored value, or <c>null</c> when there is nothing usable on disk.</summary>
    public T? Load()
    {
        if (!File.Exists(_path))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(_path);
            return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<T>(json, Options);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            QuarantineCorruptFile();
            return null;
        }
    }

    public void Save(T value)
    {
        string temp = _path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(value, Options));

        if (File.Exists(_path))
        {
            File.Replace(temp, _path, destinationBackupFileName: null);
        }
        else
        {
            File.Move(temp, _path);
        }
    }

    private void QuarantineCorruptFile()
    {
        try
        {
            File.Move(_path, _path + ".corrupt", overwrite: true);
        }
        catch (IOException)
        {
            // Nothing more we can do; the caller falls back to defaults.
        }
    }
}
