public static class EditorDefs
{
    // フォルダ構成.
    // Assets
    // ├GameDataSource(Googleスプレッドシートから生成したcsv納品フォルダ).
    // │ └Enums
    // │    └XXXDefs.csv(Enum出力用csvの配置.
    // │ ├Master
    // │ │  └XXXData.csv(GameData出力用csvの配置.
    // │ └GoogleSheetsConfig.json(Googleスプレッドシートとcsvの紐づき設定ファイル.
    // ├Scripts
    // │ └Gemerated
    // └StreamingAssets
    //   └GameData

    // GameData関連フォルダパス定義.
    public const string GameDataEnum        = "Assets/GameDataSource/Enums";
    public const string GameDataMaster      = "Assets/GameDataSource/Master";
    public const string GoogleConfigJson    = "Assets/GameDataSource/GoogleSheetsConfig.json";
    public const string GameDataBinary      = "Assets/StreamingAssets/GameData";
    public const string ScriptGenerated     = "Assets/Scripts/Generated";

}