using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

public static class GameDataEnumBuilder
{
    [MenuItem("Tools/GameData/Build Enum")]
    public static void BuildEnum()
    {
        if (!Directory.Exists(EditorDefs.GameDataEnum))
        {
            Debug.LogWarning("Enum source folder not found.");
            return;
        }

        var files = Directory.GetFiles(EditorDefs.GameDataEnum, "*.csv", SearchOption.AllDirectories);

        // フォルダがない場合はフォルダを生成.
        if (!Directory.Exists(EditorDefs.ScriptGenerated))
            Directory.CreateDirectory(EditorDefs.ScriptGenerated);

        foreach (var file in files)
        {
            GenerateEnum(file);
        }

        AssetDatabase.Refresh();

        Debug.Log("Enum build complete. Unity will recompile.");
    }

    private static void GenerateEnum(string filePath)
    {
        string enumName = Path.GetFileNameWithoutExtension(filePath);
        string[] lines = File.ReadAllLines(filePath);

        var sb = new System.Text.StringBuilder();

        sb.AppendLine("namespace GameData");
        sb.AppendLine("{");
        sb.AppendLine($"    public enum {enumName}");
        sb.AppendLine("    {");

        foreach (var line in lines.Skip(1))
        {
            var cols = line.Split(',');
            sb.AppendLine($"        {cols[0].Trim()} = {cols[1].Trim()},");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        File.WriteAllText($"{EditorDefs.ScriptGenerated}/{enumName}.cs", sb.ToString());
    }
}