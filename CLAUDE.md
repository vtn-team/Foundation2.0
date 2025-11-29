# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクト概要

Unity製ゲーム開発フレームワーク（C# / Unity 2022.3.48f1 LTS）。シーン管理、オーディオ、UI、データ管理、ネットワーク機能を提供するモジュラー設計。

## ビルド・開発環境

**セットアップ:**
1. Unity 2022.3.48f1 LTSでプロジェクトを開く
2. `unity/Setup/Download.bat`を実行してCRIオーディオプラグインを取得
3. 依存パッケージはPackage Managerで自動読み込み

**IDE:** Visual Studio 2022（Managed Game Development workload）

**アセンブリ定義:**
- `BasySystem` - メインランタイム
- `BasySystemEditor` - エディタ専用ツール

**ビルド判定:**
- エディタ: `#if UNITY_EDITOR`
- リリース: `#if RELEASE`（暗号化・本番パス用）

## アーキテクチャ

### コアシステム (Assets/Scripts/BaseSystem/)

**シーン管理 (`Foundation/Scene/`):**
- `SceneLoader.cs` - 依存解決付き非同期シーン読み込み
- `SceneDependencies.asset` - ベース＋追加シーンの関係定義
- `GameExecuterBase.cs` - シーンライフサイクルの抽象基底クラス（InitializeScene/FinalizeScene）
- シーン設定は`GameSettings.cs`で定義

**データ管理 (`Foundation/DataManagement/`):**
- `MasterData.cs` - Google Sheets APIからゲームデータを取得するシングルトン
- ローカルJSONキャッシュとバージョンチェック機能
- 読み込み: `await MasterData.LoadMasterData<T>("SheetName")`

**オーディオシステム (`CRI/`):**
- `CRIAudioManager.cs` - CRI ADX2ミドルウェアによるプロ仕様オーディオ
- チャンネル: BGMPlayer, SEPlayerWith3D, VoicePlayer
- 3D空間オーディオ対応
- `CriSoundExecuter.cs` - テストキー: Z=3D, X=SE, V=遅延SE, C=BGM切替

**ネットワーク (`Foundation/Network/`):**
- `WebRequest.cs` - カスタムヘッダー・タイムアウト対応HTTPクライアント
- `TaskRequestWorker.cs` - ワーカープール（同時5接続）
- `SequenceBridge.cs` - リクエストシーケンス制御

**UIシステム (`Foundation/UI/`):**
- `UIManager.cs` - UI統合管理
- `UIView.cs` - パネル/画面の基底クラス

**永続化 (`Foundation/IO/`):**
- `LocalData.cs` - JSON直列化（AES暗号化オプション付き）
- デバッグ: `Application.dataPath`、リリース: `Application.persistentDataPath`

**イベントシステム (`GameEvent/`):**
- `GameEventRecorder.cs` - テスト用イベント記録・再生

### 主要設定ファイル

- `GameSettings.cs` - シーンマッピング、APIエンドポイント、グローバル設定
- `BuildState.cs` - ビルドタイプ識別（チームID: "Foundation"）
- `SceneType.cs`, `ViewID.cs` - シーン/ビュー分類用列挙型

### サードパーティ依存

- **UniTask** - 非同期処理（Cysharp.Threading.Tasks）
- **Addressables** - 動的アセット読み込み
- **CRI Middleware** - ADX2プロ仕様オーディオ
- **Cinemachine** - カメラシステム
- **Universal Render Pipeline** - レンダリング
- **TextMeshPro** - テキストレンダリング

## コードパターン

- **シングルトン:** MasterData, CRIAudioManager, WebRequest
- **ワーカープール:** TaskRequestWorkerによる並行ネットワーク処理
- **テンプレートメソッド:** GameExecuterBaseのライフサイクル
- **Async/Await:** UniTaskによるノンブロッキング処理
- **部分クラス:** MasterDataは複数ファイルに分割

## シーン構成

```
Scenes/
├── System/          # GameLauncher, IngameSystem, IngameDebug, ReviewTest
├── Design/          # ゲームプレイシーン（Morning, Night）
├── Sample/          # テンプレートシーン（3DSoundSample）
├── Debug/           # デバッグシーン
└── Mockup/          # モックアップシーン
```

## 開発ワークフロー

**シーン追加:**
1. `GameSettings.cs`でベース＋追加シーンを設定
2. `Scenes/SceneDependencies.asset`に登録
3. ライフサイクルフックは`GameExecuterBase`を継承

**マスターデータ追加:**
1. Google Sheetsにシート追加
2. `SpreadSheetDataObject`継承クラスを作成
3. 読み込み: `await MasterData.LoadMasterData<YourType>("SheetName")`

**ネットワーク通信:**
```csharp
WebRequest.GetRequest(url, options);
WebRequest.PostRequest(url, body, options);
```
