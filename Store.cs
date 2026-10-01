using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FastTodo;

static class Store
{
    static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FastTodo");
    static readonly string FilePath = Path.Combine(Dir, "tasks.json");

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
