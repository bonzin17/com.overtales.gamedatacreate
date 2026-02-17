using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Text;

public static class GameDataMasterBuilder
{
    private static Dictionary<string, Type> enumTypeCache;

    [MenuItem("Tools/GameData/Build Master")]
    public static void BuildMaster()
    {
        // コンパイル中なら止める（お作法）
        if (EditorApplication.isCompiling)
        {
            Debug.LogWarning("Still compiling. Please wait.");
            return;
        }

        if (!Directory.Exists(EditorDefs.GameDataMaster))
        {
            Debug.LogWarning("Master source folder not found.");
            return;
        }

        // フォルダがない場合はフォルダを生成.
        if (!Directory.Exists(EditorDefs.ScriptGenerated))
            Directory.CreateDirectory(EditorDefs.ScriptGenerated);
        if (!Directory.Exists(EditorDefs.GameDataBinary))
            Directory.CreateDirectory(EditorDefs.GameDataBinary);


        BuildEnumCache();   // ここで型キャッシュ構築

        var files = Directory.GetFiles(EditorDefs.GameDataMaster, "*.csv", SearchOption.AllDirectories);
        {
            GenerateMasters(files);
        }

        AssetDatabase.Refresh();

        Debug.Log("Master build complete.");
    }

    private static void BuildEnumCache()
    {
        enumTypeCache = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsEnum && t.Namespace == "GameData")
            .ToDictionary(t => t.Name, t => t);
    }

    private static void GenerateMasters(string[] files)
    {
        foreach (var file in files)
        {
            string className = Path.GetFileNameWithoutExtension(file);
            string[] lines = File.ReadAllLines(file);

            string[] headers = lines[0].Split(',');     // 列の名称.
            string[] types = lines[1].Split(',');       // データの型.
            string[] rows = lines.Skip(2).ToArray();    // データの内容.

            // csv内容検証関数.
            ValidateCsvTypes(className, headers, types, rows);
            ValidateEnumCoverage(className, types, rows);

            // コード・データの出力.
            GenerateDataClass(className, headers, types);
            GenerateBinaryClass(className, headers, types);
            GenerateBinaryFile(className, headers, types, rows);
        }
    }

    private static void GenerateDataClass(string className, string[] headers, string[] types)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("using System;");
        sb.AppendLine("namespace GameData");
        sb.AppendLine("{");
        sb.AppendLine("    [Serializable]");
        sb.AppendLine($"    public class {className}");
        sb.AppendLine("    {");

        for (int i = 0; i < headers.Length; i++)
            sb.AppendLine($"        public {types[i]} {headers[i]};");

        sb.AppendLine("    }");
        sb.AppendLine("}");

        File.WriteAllText($"{EditorDefs.ScriptGenerated}/{className}.cs", sb.ToString());
    }

    private static void GenerateBinaryFile(
    string className,
    string[] headers,
    string[] types,
    string[] rows)
    {
        string path = $"{EditorDefs.GameDataBinary}/{className}.bytes";

        using var fs = new FileStream(path, FileMode.Create);
        using var writer = new BinaryWriter(fs);

        writer.Write(rows.Length);

        foreach (var row in rows)
        {
            var cols = row.Split(',');

            for (int i = 0; i < types.Length; i++)
            {
                WriteValue(writer, types[i], cols[i]);
            }
        }
    }

    private static void WriteValue(BinaryWriter writer, string type, string value)
    {
        switch (type)
        {
            case "int":
                writer.Write(int.Parse(value));
                break;

            case "float":
                writer.Write(float.Parse(value));
                break;

            case "bool":
                writer.Write(bool.Parse(value));
                break;

            case "string":
                writer.Write(value);
                break;

            default:
                //var enumType = GetEnumType(type);
                var enumType = enumTypeCache[type];

                if (enumType == null)
                    throw new Exception($"Enum type not found: {type}{value}");

                writer.Write((int)Enum.Parse(enumType, value));
                break;
        }
    }

    private static Type GetEnumType(string typeName)
    {
        Debug.Log($"GetEnumType : {typeName}");
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = assembly.GetType($"GameData.{typeName}");
            if (t != null)
                return t;
        }

        return null;
    }
    private static void GenerateBinaryClass(
    string className,
    string[] headers,
    string[] types)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.IO;");
        sb.AppendLine("");
        sb.AppendLine("namespace GameData");
        sb.AppendLine("{");
        sb.AppendLine($"    public static class {className}Binary");
        sb.AppendLine("    {");

        // -------------------------
        // Write
        // -------------------------
        sb.AppendLine($"        public static void Write(BinaryWriter writer, {className} data)");
        sb.AppendLine("        {");

        for (int i = 0; i < headers.Length; i++)
        {
            sb.AppendLine("            " + GetWriteCode(types[i], headers[i]));
        }

        sb.AppendLine("        }");
        sb.AppendLine("");

        // -------------------------
        // Read
        // -------------------------
        sb.AppendLine($"        public static {className} Read(BinaryReader reader)");
        sb.AppendLine("        {");
        sb.AppendLine($"            {className} data = new {className}();");
        sb.AppendLine("");

        for (int i = 0; i < headers.Length; i++)
        {
            sb.AppendLine("            " + GetReadCode(types[i], headers[i]));
        }

        sb.AppendLine("");
        sb.AppendLine("            return data;");
        sb.AppendLine("        }");
        sb.AppendLine("");

        // -------------------------
        // ReadList
        // -------------------------
        sb.AppendLine($"        public static List<{className}> ReadList(BinaryReader reader)");
        sb.AppendLine("        {");
        sb.AppendLine("            int count = reader.ReadInt32();");
        sb.AppendLine($"            List<{className}> list = new List<{className}>(count);");
        sb.AppendLine("");
        sb.AppendLine("            for (int i = 0; i < count; i++)");
        sb.AppendLine("            {");
        sb.AppendLine("                list.Add(Read(reader));");
        sb.AppendLine("            }");
        sb.AppendLine("");
        sb.AppendLine("            return list;");
        sb.AppendLine("        }");

        sb.AppendLine("    }");
        sb.AppendLine("}");

        File.WriteAllText($"{EditorDefs.ScriptGenerated}/{className}Binary.cs", sb.ToString());
    }

    private static string GetWriteCode(string type, string field)
    {
        switch (type)
        {
            case "int":
            case "float":
            case "bool":
            case "string":
                return $"writer.Write(data.{field});";

            default:
                // enum
                return $"writer.Write((int)data.{field});";
        }
    }

    private static string GetReadCode(string type, string field)
    {
        switch (type)
        {
            case "int":
                return $"data.{field} = reader.ReadInt32();";

            case "float":
                return $"data.{field} = reader.ReadSingle();";

            case "bool":
                return $"data.{field} = reader.ReadBoolean();";

            case "string":
                return $"data.{field} = reader.ReadString();";

            default:
                // enum
                return $"data.{field} = ({type})reader.ReadInt32();";
        }
    }

    private static void ValidateCsvTypes(
    string className,
    string[] headers,
    string[] types,
    string[] rows)
    {
        if (headers.Length != types.Length)
            throw new Exception($"{className}: Header count and type count mismatch.");

        for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            var cols = rows[rowIndex].Split(',');

            if (cols.Length != types.Length)
                throw new Exception(
                    $"{className}: Column count mismatch at row {rowIndex + 3}");

            for (int colIndex = 0; colIndex < types.Length; colIndex++)
            {
                string typeName = types[colIndex];
                string value = cols[colIndex];

                if (!ValidateValue(typeName, value))
                {
                    throw new Exception(
                        $"{className}: Type mismatch at row {rowIndex + 3}, column '{headers[colIndex]}' ({typeName}) : value = '{value}'");
                }
            }
        }
    }

    private static bool ValidateValue(string typeName, string value)
    {
        switch (typeName)
        {
            case "int":
                return int.TryParse(value, out _);

            case "float":
                return float.TryParse(value, out _);

            case "bool":
                return bool.TryParse(value, out _);

            case "string":
                return true;

            default:
                // Enum判定
                if (!enumTypeCache.TryGetValue(typeName, out var enumType))
                    return false;

                return Enum.IsDefined(enumType, value);
        }
    }

    private static void ValidateEnumCoverage(
    string className,
    string[] types,
    string[] rows)
    {
        for (int colIndex = 0; colIndex < types.Length; colIndex++)
        {
            if (!enumTypeCache.TryGetValue(types[colIndex], out var enumType))
                continue;

            var enumValues = new HashSet<string>(
                Enum.GetNames(enumType));

            var csvValues = new HashSet<string>();

            foreach (var row in rows)
            {
                var cols = row.Split(',');
                csvValues.Add(cols[colIndex]);
            }

            foreach (var e in enumValues)
            {
                if (!csvValues.Contains(e))
                {
                    throw new Exception(
                        $"{className}: Enum '{e}' of {enumType.Name} not defined in CSV");
                }
            }
        }
    }

}

