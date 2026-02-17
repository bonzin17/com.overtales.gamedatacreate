#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.IO;
using System.Collections;

using System;

[Serializable]
public class GoogleSheetEntry
{
    public string spreadsheetId;
    public string gid;
    public string sheetName;
}

[Serializable]
public class GoogleSheetConfig
{
    public GoogleSheetEntry[] sheets;
}

public class GoogleSheetDownloader : EditorWindow
{
    [MenuItem("Tools/Download Google Sheets CSV")]
    public static void Download()
    {
        DownloadAll();
    }

    private static void DownloadAll()
    {
        if (!File.Exists(EditorDefs.GoogleConfigJson))
        {
            Debug.LogError("Config file not found.");
            return;
        }

        string json = File.ReadAllText(EditorDefs.GoogleConfigJson);
        GoogleSheetConfig config = JsonUtility.FromJson<GoogleSheetConfig>(json);

        foreach (var sheet in config.sheets)
        {
            DownloadCsv(sheet);
        }

        AssetDatabase.Refresh();
        Debug.Log("Download Complete");
    }

    private static void DownloadCsv(GoogleSheetEntry sheet)
    {
        string url =
            $"https://docs.google.com/spreadsheets/d/{sheet.spreadsheetId}/export?format=csv&gid={sheet.gid}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            var operation = request.SendWebRequest();
            while (!operation.isDone) { }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed: {sheet.sheetName} - {request.error}");
                return;
            }

            // ファイル名の末尾がDefsならEnumフォルダ、それ以外ならMasterフォルダへ納品.
            bool isEnum = IsEnumFile(sheet.sheetName);
            string fullFolderPath = isEnum ? EditorDefs.GameDataEnum : EditorDefs.GameDataMaster;
            if (!Directory.Exists(fullFolderPath))
                Directory.CreateDirectory(fullFolderPath);

            string path = Path.Combine(fullFolderPath, sheet.sheetName + ".csv");
            File.WriteAllText(path, request.downloadHandler.text);

            Debug.Log($"Saved: {sheet.sheetName}.csv");
        }
    }

    private static bool IsEnumFile(string sheetName)
    {
        if (sheetName.EndsWith("Defs"))
        {
            return true;
        }
        return false;
    }
}
#endif
