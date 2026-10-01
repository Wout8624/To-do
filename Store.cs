using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FastTodo;

static class Store
{
    static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FastTodo");
    static readonly string FilePath = Path.Combine(Dir, "tasks.json");
    static readonly string ZoomPath = Path.Combine(Dir, "zoom.txt");

    public static double LoadZoom() =>
        File.Exists(ZoomPath) &&
        double.TryParse(File.ReadAllText(ZoomPath), NumberStyles.Float, CultureInfo.InvariantCulture, out var z) ? z : 1;

    public static void SaveZoom(double zoom)
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllText(ZoomPath, zoom.ToString(CultureInfo.InvariantCulture));
    }

    public static List<TodoItem> Load()
    {
        if (!File.Exists(FilePath)) return [];
        try
        {
            return JsonSerializer.Deserialize(File.ReadAllBytes(FilePath), JsonCtx.Default.ListTodoItem) ?? [];
        }
        catch (JsonException)
        {
            // Never silently overwrite a damaged file: keep a copy, start fresh.
            File.Copy(FilePath, FilePath + ".corrupt", overwrite: true);
            return [];
        }
    }

    // Write to a temp file, then swap: a crash mid-write can never corrupt tasks.json.
    public static void Save(IEnumerable<TodoItem> items)
    {
        Directory.CreateDirectory(Dir);
        var tmp = FilePath + ".tmp";
        File.WriteAllBytes(tmp, JsonSerializer.SerializeToUtf8Bytes(items.ToList(), JsonCtx.Default.ListTodoItem));
        File.Move(tmp, FilePath, overwrite: true);
    }
}

// Compile-time JSON serializer: no reflection at startup.
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(List<TodoItem>))]
partial class JsonCtx : JsonSerializerContext { }
