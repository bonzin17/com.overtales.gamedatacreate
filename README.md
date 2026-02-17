■概要<br>
これはUnity用のバイナリベースのゲームデータ管理システムです
本パッケージは以下を提供します

A．Googleスプレッドシートをcsvファイルとしてをローカルに保存する機能<br>
B．保存されたcsvをもとに、Unityで扱うゲームデータバイナリ、ゲームデータクラス、Enumを生成する機能<br>
C．ゲームデータバイナリを読み込みインゲームでデータへアクセスする機能<br>

======================================================================

■導入方法<br>
Packages/manifest.json に以下を追加してください：
```json
"com.overtales.gamedatacreate": "git+https://github.com/bonzin17/com.overtales.gamedatacreate.git#1.0.0"
```

======================================================================

■フォルダ構成<br>

```
    // com.overtales.gamedatacreate
    // ├Editor
    // │ ├EditorDefs.cs
    // │ ├GameDataEnumBuilder.cs
    // │ ├GameDataMasterBuilder.cs
    // │ ├GoogleSheetDownloader.cs
    // │ └GameDataCreateSystem.Editor.asmdef
    // ├Runtime
    // │ └Gemerated
    // │   ├GameDataManager.cs
    // │   └GameDataCreateSystem.asmdef
```

======================================================================

■前提条件<br>
これを使用するには、出力ファイルに関わるフォルダ構成を以下に固定する必要があります

```
    // フォルダ構成.
    // Assets
    // ├GameDataSource(Googleスプレッドシートから生成したcsv納品フォルダ).
    // │ ├Enums
    // │ │  └XXXDefs.csv(Enum出力用csvの配置.
    // │ ├Master
    // │ │  └XXXData.csv(GameData出力用csvの配置.
    // │ └GoogleSheetsConfig.json(Googleスプレッドシートとcsvの紐づき設定ファイル.
    // ├Scripts
    // │ └Gemerated
    // │   ├XXXDefs.cs(csvから生成されたEnum.
    // │   └XXXData.cs(csvから生成されたデータクラス.
    // └StreamingAssets
    // │ └GameData
    // │   ├XXXData.bytes(csvから生成されたバイナリデータ.
```


======================================================================

■使用方法A：Googleスプレッドシートからcsvを生成する<br>

〇事前手順<br>
GoogleSheetsConfig.jsonにフォーマットを守って、ダウンロードしたいシートの情報を記載してください

以下に参考のテンプレを記載します
```json
{
  "sheets": [
    {
      "spreadsheetId": "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
      "gid": "0",
      "sheetName": "EnemyData"
    },
    {
      "spreadsheetId": "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
      "gid": "1234567890",
      "sheetName": "EnemyDefs"
    },
    {
      "spreadsheetId": "ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ",
      "gid": "0",
      "sheetName": "CharacterTypeDefs"
    },
  ]
}
```
spreadsheetIdにはURLからわかるスプレッドシートのIDを<br>
gidはシートのIDを<br>
sheetNameにはインゲームで扱うデータの名前を記載してください<br>

sheetNameは命名規則があり"XXXDefs"と記載した場合はEnumを"XXXData"と記載した場合はDataを生成します

〇スプレッドシートの入力ルール・その１<br>
シート名を"XXXData"としたシートの入力ルールです<br>
Dataを自動生成する都合上、スプレッドシートへのデータ入力には入力ルールがあります<br>

以下の例を参考にルールを守ってください

```
id	chara	hp	atk	def	spd
EnemyDefs	string	int	int	int	int
Invalid	無効	0	0	0	0
Slime	スライム	10	2	1	3
Dragon	ドラゴン	100	10	10	10
Warm	ワーム	200	4	5	1
```
1行目は変数名です。半角英数字の入力を必須とします<br>
2行目は型名です。Enum名、string、int、float、boolをサポートします<br>
3行目以降はデータ入力行です。列数を1-2行目と合わせてください<br>

〇スプレッドシートの入力ルール・その２<br>
シート名を"XXXDefs"としたシートの入力ルールです<br>
Enumを自動生成する都合上、スプレッドシートへのデータ入力には入力ルールがあります<br>

以下の例を参考にルールを守ってください

```
ValueName	Value
Invalid	0
Slime	1
Dragon	2
Warm	3
```

1行目は固定で、ValueName	Valueとしてください<br>
2行目以降は定義入力行です<br>
1列目に定義名を、2列目に定義に対応する数値を記載してください<br>

〇使用手順<br>
Tools→DownLoadGoogleSheetsCSVをクリックしてください

======================================================================

■使用方法B：csvからゲームデータ、データクラス、定義を生成する<br>
〇事前手順<br>
使用方法Aでフォーマットに従ったcsvを指定のフォルダに配置してください

〇使用手順<br>
Tools→GameData→Build Enum をクリックしてください
Tools→GameData→Build Master をクリックしてください

〇注意事項<br>
Enumがない状態でBuildMasterを行うと、存在しないEnumへのアクセスでエラーが発生する可能性があります
csvの入力フォーマットに問題がある場合はエラーにかかります

======================================================================

■使用方法C：インゲームからバイナリを読み込み、データへアクセスする<br>
〇事前手順<br>
使用方法Bでゲームデータ、データクラス、定義を生成済みであることを確認してください

〇使用手順<br>
①全ゲームデータバイナリの読み込みと登録
```C#
GameDataManager.Instance.Initialize();
```

②データ取得と使用
```C#
var character = GameDataManager.Instance.Get<CharacterType,CharacterTypeData>(CharacterType.Warrior);
Debug.Log(character.Name);
```
======================================================================

■設計思想<br>
１．中～小規模向けのチーム向けの機能です<br>
２．なるべく無料の機能を活用してゲームデータを作成できるようにしています<br>
３．フォーマットを守ってもらう必要はありますが、データや定義は各プロジェクトが管理できます<br>
４．パッケージは「読込」と「保持」を担当します<br>
５．Enumをキーとすることで型安全を保障したつもりです<br>

======================================================================
■ バージョンポリシー

セマンティックバージョニング（SemVer）を採用すします。<br>
Patch（例: 1.0.1）<br>
  バグ修正<br>
Minor（例: 1.1.0）<br>
  後方互換のある機能追加<br>
Major（例: 2.0.0）<br>
  破壊的変更<br>
