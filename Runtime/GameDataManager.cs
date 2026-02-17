using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

public class GameDataManager
{
    private static GameDataManager _instance;
    public static GameDataManager Instance => _instance ??= new GameDataManager();

    // DataType → Dictionary<Enum, TData>
    private Dictionary<Type, object> _masterData = new();

    private bool _initialized;

    private GameDataManager() { }

    // =====================================
    // 初期化
    // =====================================
    public void Initialize()
    {
        if (_initialized)
            return;

        LoadAllMasters();

        _initialized = true;
    }

    // =====================================
    // 全Master自動ロード
    // =====================================
    private void LoadAllMasters()
    {
        string folder = Path.Combine(Application.streamingAssetsPath, "GameData");

        var files = Directory.GetFiles(folder, "*.bytes");

        foreach (var file in files)
        {
            string className = Path.GetFileNameWithoutExtension(file);

            Type dataType = GetGameDataType(className);
            Type binaryType = GetBinaryType(className);

            if (dataType == null || binaryType == null)
            {
                Debug.LogWarning($"Type not found: {className}");
                continue;
            }

            using var fs = new FileStream(file, FileMode.Open);
            using var reader = new BinaryReader(fs);

            MethodInfo readListMethod = binaryType.GetMethod("ReadList");
            var listObj = readListMethod.Invoke(null, new object[] { reader });

            CreateDictionary(dataType, listObj);
        }
    }

    // =====================================
    // Dictionary生成（先頭Enumフィールドをキー）
    // =====================================
    private void CreateDictionary(Type dataType, object listObj)
    {
        var list = listObj as System.Collections.IEnumerable;
        if (list == null)
            throw new Exception($"Invalid list for {dataType.Name}");

        // 先頭フィールド取得
        var fields = dataType.GetFields(BindingFlags.Public | BindingFlags.Instance);
        if (fields.Length == 0)
            throw new Exception($"{dataType.Name} has no fields.");

        var keyField = fields[0];

        if (!keyField.FieldType.IsEnum)
            throw new Exception($"{dataType.Name} first field must be Enum.");

        Type enumType = keyField.FieldType;

        // Dictionary<EnumType, DataType> を動的生成
        Type dictType = typeof(Dictionary<,>).MakeGenericType(enumType, dataType);
        var dict = Activator.CreateInstance(dictType);

        MethodInfo addMethod = dictType.GetMethod("Add");

        foreach (var item in list)
        {
            var key = keyField.GetValue(item);

            if (key == null)
                throw new Exception($"Null enum key in {dataType.Name}");

            addMethod.Invoke(dict, new object[] { key, item });
        }

        _masterData[dataType] = dict;
    }

    // =====================================
    // データ取得
    // =====================================
    public TData Get<TEnum, TData>(TEnum key)
        where TEnum : Enum
    {
        if (!_masterData.TryGetValue(typeof(TData), out var dictObj))
            throw new Exception($"{typeof(TData).Name} not loaded.");

        var dict = dictObj as Dictionary<TEnum, TData>;

        if (dict == null)
            throw new Exception($"Dictionary type mismatch for {typeof(TData).Name}");

        return dict[key];
    }

    // =====================================
    // Type取得
    // =====================================
    private Type GetGameDataType(string className)
    {
        return Type.GetType($"GameData.{className}");
    }

    private Type GetBinaryType(string className)
    {
        return Type.GetType($"GameData.{className}Binary");
    }
}
