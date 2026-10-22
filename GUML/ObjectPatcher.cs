using System.Text.Json;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace GamemakerUniversalModLoader;

public class ObjectPatcher(UndertaleData data, string modDir)
{
    private readonly string _objectPath = Path.Combine(modDir, "objects");

    public bool Exist()
    {
        return Directory.Exists(_objectPath);
    }

    public void Execute()
    {
        if (!Exist()) return;
        var files = Directory.EnumerateFiles(_objectPath, "*.json");
        foreach (var objJson in files)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(objJson));
            var root = document.RootElement;
            var objectPatch = new ObjectPatch
            {
                Name = ReadString(root, "Name"),
                Parent = ReadString(root, "Parent"),
                Awake = root.TryGetProperty("Awake", out var awake) && awake.ValueKind == JsonValueKind.True
            };
            if (string.IsNullOrEmpty(objectPatch.Name)) continue;

            // Don't add a duplicate if the object already exists (e.g. the loader is being
            // re-applied to an already patched data file). Duplicate objects would make
            // asset references and event linking resolve to different objects.
            var existing = data.GameObjects.ByName(objectPatch.Name);
            if (existing is not null)
            {
                existing.ParentId = data.GameObjects.ByName(objectPatch.Parent);
                existing.Awake = objectPatch.Awake;
                continue;
            }

            var obj = new UndertaleGameObject();
            obj.Name = data.Strings.MakeString(objectPatch.Name);
            obj.ParentId = data.GameObjects.ByName(objectPatch.Parent);
            obj.Awake = objectPatch.Awake;
            data.GameObjects.Add(obj);
        }
    }

    private static string ReadString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }
}
public class ObjectPatch
{
    public string Name { get; set; }
    public string Parent { get; set; }
    public bool Awake { get; set; }
}
