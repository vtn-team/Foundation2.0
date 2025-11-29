# InjectSystemクラス概要

# 概要
- 注入用パラメータを保持しておくシングルトン

# 実装
- シーン読み込み時、またはゲーム起動時に注入用のパラメータを読み込んでおく

# 処理フロー
1. シーン読み込み時、またはゲーム起動時にDataAsset/ParamInjectSettingsを読み込む
	1. 直接パス読み込み (AssetDatabase.LoadAssetAtPath) を優先
	2. フォールバック: Resources フォルダから読み込み
	3. **ParamInjectSettingsが見つからない場合、自動生成を実行**
2. 各注入クラスからアクセスされる

## ParamInjectSettings自動生成機能
- **目的**: ParamInjectSettingsファイルの不存在によるエラーを防止し、セットアップを簡素化
- **動作条件**: Unity Editorモードでのみ実行
- **自動生成内容**:
  - `Assets/DataAsset/Params/ParamInjectSettings.asset` を作成
  - デフォルト値: AutoGenerate=true, GeneratedCodePath="Assets/Scripts/Inject/Generated/"
  - 必要に応じてディレクトリも自動作成
- **手動生成**: InjectSystemコンポーネントのコンテキストメニューから実行可能

# 内部変数
- paramInjectSettings: 読み込んだParamInjectSettings

# 外部インタフェース
- ParamInjectSettingsを取得する
- IsParamInjectSettingsAvailable: ParamInjectSettingsが利用可能かを確認

# 期待値
- ユースケースに応じて注入するパラメータを変化させることができる

# エッジケース
- ParamInjectSettingsファイルが存在しない場合: 自動生成機能により解決
- ビルド時（Editorモード以外）にファイルが存在しない場合: nullを返し、Inject処理をスキップ
