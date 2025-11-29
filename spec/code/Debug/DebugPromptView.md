# DebugPromptViewクラス設計

## 概要
- デバッグメニューのUI表示を担当するMonoBehaviour
- **UI Toolkitを使用**してゲーム中にオーバーレイ表示
- コマンドに応じたウィジェットを動的に生成
- 他のオーバーレイUIの参考実装として利用可能
- ハイブリッド方式：基本レイアウトはUXML、動的要素はコードで生成

## クラス構成

### DebugPromptView : MonoBehaviour
デバッグメニューUIの表示・管理を行うコンポーネント

## インスペクタ設定

### UI Document
| プロパティ | 型 | 説明 |
|---|---|---|
| uiDocument | UIDocument | UI表示用のUIDocumentコンポーネント |

### UXML Templates
| プロパティ | 型 | 説明 |
|---|---|---|
| mainTemplate | VisualTreeAsset | メインレイアウトのUXMLテンプレート |
| buttonTemplate | VisualTreeAsset | ボタンウィジェットのUXMLテンプレート |
| toggleTemplate | VisualTreeAsset | トグルウィジェットのUXMLテンプレート |

### Settings
| プロパティ | 型 | デフォルト値 | 説明 |
|---|---|---|---|
| sortingOrder | int | 30000 | UIDocumentの描画順 |

## プロパティ

| プロパティ | 型 | 説明 |
|---|---|---|
| IsVisible | bool | 表示中かどうか |
| OnCommandExecuted | Observable<DebugCommand> | コマンド実行時のイベント |

## メソッド

| メソッド | 戻り値 | 説明 |
|---|---|---|
| Show() | void | デバッグメニューを表示 |
| Hide() | void | デバッグメニューを非表示 |
| BuildMenu(IReadOnlyList<DebugCommand>) | void | コマンドリストからUIを構築 |
| SetTitle(string) | void | メニューのタイトルを設定 |
| ClearWidgets() | void | 全てのウィジェットをクリア |
| CreateLabel(string) | void | ラベルウィジェットを作成 |

## ウィジェット生成ロジック

コマンドの型に応じて適切なウィジェットを生成：

| コマンド型 | 生成ウィジェット |
|---|---|
| DebugInvincibleCommand | Toggle |
| DebugCommand (その他) | Button |

## UI Toolkit構成

### UXMLファイル
- `DebugPromptView.uxml` - メインレイアウト
- `DebugPromptView_ButtonTemplate.uxml` - ボタンテンプレート
- `DebugPromptView_ToggleTemplate.uxml` - トグルテンプレート

### USSファイル
- `DebugPromptView.uss` - スタイル定義

### UI要素構造
```
overlay-root (画面全体を覆う半透明背景)
└── menu-panel (メニューパネル)
    ├── header (ヘッダー)
    │   └── title-text (タイトル)
    └── scroll-view (スクロールビュー)
        └── content-container (動的要素コンテナ)
```

## フォールバック機能
- UXMLテンプレートが未設定の場合、コードで動的にUIを生成
- デザイナーがUXMLを編集可能、開発者はコードのみでも動作可能

## 依存関係
- UnityEngine.UIElements
- R3 (リアクティブプログラミング)

## ファイルパス
- `unity/Assets/Scripts/Debug/DebugPromptView.cs`
- `unity/Assets/Scripts/Debug/DebugPromptView.uxml`
- `unity/Assets/Scripts/Debug/DebugPromptView.uss`
- `unity/Assets/Scripts/Debug/DebugPromptView_ButtonTemplate.uxml`
- `unity/Assets/Scripts/Debug/DebugPromptView_ToggleTemplate.uxml`
