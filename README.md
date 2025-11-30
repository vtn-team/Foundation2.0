# Foundation

Unity製ゲーム開発フレームワーク。シーン管理、オーディオ、UI、データ管理、ネットワーク機能を提供するモジュラー設計のベースシステムです。

## 概要

Foundationは、Unityゲーム開発における共通機能を再利用可能な形で提供するフレームワークです。プロジェクトの立ち上げを迅速化し、堅牢なアーキテクチャの基盤を提供します。

### 主な特徴

- **シーン管理** - 依存関係解決付きの非同期シーン読み込み
- **オーディオシステム** - CRI ADX2ミドルウェアによるプロ仕様サウンド
- **データ管理** - Google Sheets連携によるマスターデータ管理
- **ネットワーク** - ワーカープール対応HTTPクライアント
- **UIシステム** - 統合UI管理とビューコンポーネント
- **永続化** - AES暗号化対応のローカルデータ保存

## 必要環境

| 要件 | バージョン |
|------|------------|
| Unity | 2022.3.48f1 LTS |
| IDE | Visual Studio 2022 (Managed Game Development workload) |
| OS | Windows 10/11 |

## セットアップ

### 1. リポジトリのクローン

```bash
git clone https://github.com/your-org/Foundation.git
cd Foundation
```

### 2. Unity プロジェクトを開く

Unity Hub から `unity` フォルダを開きます。

### 3. CRI オーディオプラグインの取得

```bash
# Windows
unity/Setup/Download.bat
```

### 4. 依存パッケージの確認

Package Manager で以下のパッケージが自動的に読み込まれます：

- Addressables (1.22.2)
- Cinemachine (2.10.1)
- Input System (1.11.1)
- Universal Render Pipeline (14.0.11)
- TextMeshPro (3.0.9)
- Timeline (1.7.6)

## プロジェクト構造

```
Foundation/
├── unity/                      # Unity プロジェクト
│   ├── Assets/
│   │   ├── Scripts/
│   │   │   ├── BaseSystem/     # メインランタイム
│   │   │   │   ├── CRI/        # オーディオシステム
│   │   │   │   ├── Foundation/ # コア機能
│   │   │   │   │   ├── DataManagement/
│   │   │   │   │   ├── IO/
│   │   │   │   │   ├── Network/
│   │   │   │   │   ├── Scene/
│   │   │   │   │   └── UI/
│   │   │   │   ├── GameEvent/  # イベントシステム
│   │   │   │   └── Dynamic/    # 動的設定
│   │   │   └── BaseSystemEditor/  # エディタ専用
│   │   ├── Scenes/             # シーンファイル
│   │   ├── Prefabs/            # プレハブ
│   │   └── DataAsset/          # データアセット
│   ├── Packages/               # Unity パッケージ
│   └── Setup/                  # セットアップスクリプト
├── spec/                       # 仕様書
│   ├── code/                   # コード仕様
│   ├── gamedesign/             # ゲームデザイン仕様
│   ├── rule/                   # ルール・規約
│   ├── ubidic/                 # 用語辞書
│   └── usecase/                # ユースケース
├── script/                     # 補助スクリプト
└── .github/workflows/          # CI/CD 設定
```

## アセンブリ構成

| アセンブリ | 説明 |
|-----------|------|
| `BasySystem` | メインランタイム |
| `BasySystemEditor` | エディタ専用ツール |

## コアシステム

### シーン管理

依存解決付きの非同期シーン読み込みを提供します。

```csharp
// シーンの読み込み
await SceneLoader.LoadSceneAsync(SceneType.Title);
```

**関連ファイル:**
- `SceneLoader.cs` - シーン読み込み
- `SceneDependencies.asset` - 依存関係定義
- `GameExecuterBase.cs` - ライフサイクル基底クラス

### データ管理

Google Sheets APIからゲームデータを取得します。

```csharp
// マスターデータの読み込み
var enemies = await MasterData.LoadMasterData<EnemyData>("Enemy");
```

### オーディオシステム

CRI ADX2による高品質オーディオ再生。

```csharp
// BGM再生
CRIAudioManager.Instance.BGMPlayer.Play("bgm_title");

// SE再生（3D空間対応）
CRIAudioManager.Instance.SEPlayerWith3D.Play("se_hit", position);
```

### ネットワーク

```csharp
// GETリクエスト
var response = await WebRequest.GetRequest(url, options);

// POSTリクエスト
var response = await WebRequest.PostRequest(url, body, options);
```

### 永続化

```csharp
// データ保存
LocalData.Save("save_key", saveData);

// データ読み込み
var data = LocalData.Load<SaveData>("save_key");
```

## 追加機能

詳細は [feature_list.md](./feature_list.md) を参照してください。

| 機能 | 状態 | 説明 |
|------|------|------|
| クォンタイズエンジン | 未検証 | 音楽同期再生タイミング調整 |
| シンプルDI | 使用中 | パラメータ注入システム |
| ブラックボード | 未検証 | 値の一元管理 |
| Prefab辞書 | 使用中 | オブジェクトプール管理 |
| エフェクト再生 | 未検証 | パーティクル管理 |
| ヒットストップ | 未検証 | 時間制御 |
| キューシステム | 未検証 | 演出シーケンス自動生成 |
| ボックスガチャ | 未検証 | 重みづけ抽選 |
| デバッグプロンプト | 未検証 | ゲーム内デバッグコマンド |

## 開発ガイドライン

### シーンの追加

1. `GameSettings.cs` でベース＋追加シーンを設定
2. `Scenes/SceneDependencies.asset` に登録
3. `GameExecuterBase` を継承してライフサイクルフックを実装

### マスターデータの追加

1. Google Sheets にシートを追加
2. `SpreadSheetDataObject` 継承クラスを作成
3. `MasterData.LoadMasterData<T>()` で読み込み

### ビルド判定

```csharp
#if UNITY_EDITOR
    // エディタ専用コード
#endif

#if RELEASE
    // リリースビルド専用（暗号化・本番パス）
#endif
```

## CI/CD

GitHub Actions による日次実装レビューワークフローを設定しています。

- **実行タイミング**: 毎日 JST 4:00
- **機能**: 前回実行以降の変更をサマライズしてSlackに投稿
- **実行環境**: Self-Hosted Runner (Windows)

## サードパーティライブラリ

| ライブラリ | 用途 |
|-----------|------|
| [UniTask](https://github.com/Cysharp/UniTask) | 非同期処理 |
| [CRI Middleware](https://www.cri-mw.co.jp/) | ADX2 オーディオ |
| Addressables | 動的アセット読み込み |
| Cinemachine | カメラシステム |
| Universal Render Pipeline | レンダリング |
| TextMeshPro | テキストレンダリング |

## ドキュメント

- [仕様書](./spec/README.md) - 詳細仕様
- [機能リスト](./feature_list.md) - 実装済み機能一覧
- [CLAUDE.md](./CLAUDE.md) - AI開発支援用ガイド

## コントリビューション

1. `feature/` ブランチを作成
2. 変更をコミット
3. Pull Request を作成

## ライセンス

プロジェクト固有のライセンスが適用されます。詳細はプロジェクト管理者にお問い合わせください。

---

**チームID**: Foundation
