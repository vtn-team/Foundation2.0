# InjectSelectorクラス設計

# 概要
- ParamInjectSettingsのエディタ拡張
- UnityEditorのみ参照される


# 必要要件
- Editorを継承する
- ParamInjectSettingsを対象とする


# 実装
- プロジェクト内にあるInjectParamListのリストを表示する
- メニューからアクティブなInjectParamListを切り替えられるようにする
- ゲーム再生中は何もしない
- ユーザ操作がない限りParamInjectSettingsは変更しないこと


# 初期化/起動
- プロジェクト内にあるInjectParamListのリストを生成する

## 処理
- InjectParamListのリスト(listNameとdescription)を羅列し、Selectボタンをそれぞれに用意する
- Selectボタンを押すと、「パラメータ設定」を行う

### プロジェクト内にあるInjectParamListのリストを生成する
1. プロジェクト内にあるInjectParamListを検索する
2. InjectParamListからlistNameとdescriptionを取得し、リスト化する
3. (2)をコードとして保存する
	1. このコードは、「メニューからアクティブなInjectParamListを切り替えられるようにする」の要件を満たす必要がある
	2. メニューから選択した場合、「パラメータ設定」を行う

## パラメータ設定
- ParamInjectSettings(Assets/DataAsset/Params/ParamInjectSettings.assetにある。パスは固定。)のSelected Param Listを渡されたデータにする。


# 実装ファイル

## InjectSelector.cs
- ParamInjectSettingsのCustomEditor
- AssetDatabaseを使用してInjectParamListを検索
- インスペクタ上でリスト表示とSelectボタンを提供
- Undo/Redo対応

## InjectMenuGenerator.cs
- InjectParamListの動的メニュー生成を管理
- Unity起動時に自動的にメニューを生成
- `Tool/Inject/Select Param List/`配下にメニューアイテムを作成
- 生成されるメニューコードは`Assets/Scripts/Editor/Inject/Generated/InjectParamListMenu.cs`に出力
- partial classを使用してInjectParamListMenuクラスを分割
	- 自動生成部分: メニューアイテム定義
	- 手動作成部分: SelectParamListメソッド（実際のパラメータ設定処理）

## InjectParamListMenu.cs（自動生成）
- InjectMenuGeneratorによって自動生成される
- 各InjectParamListのメニューアイテムを定義
- partial classとしてInjectMenuGeneratorの手動作成部分と結合される