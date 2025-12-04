# Melpomene - GitHub Issues連携デバッグツール

## 概要
ゲーム画面上から直接GitHub Issuesにチケットを作成・閲覧できるデバッグツール。
シーン上のオブジェクトに紐づけたバグ報告や改善要望を管理する。

## ツール名
- **Melpomene**（メルポメネー）- ギリシャ神話の悲劇のミューズにちなむ

## ファイル構成
```
unity/Assets/Foundation/Scripts/BaseSystemEditor/Debug/Melpomene/
├── MelpomeneManager.cs          # メイン管理クラス
├── MelpomeneTicket.cs           # チケットデータクラス
├── MelpomeneGitHubClient.cs     # GitHub API クライアント
├── MelpomeneCache.cs            # キャッシュ管理
├── MelpomeneConfig.asset        # 設定アセット
├── UI/
│   ├── MelpomeneWindow.cs       # EditorWindow（SceneView統合）
│   ├── MelpomeneInputView.cs    # チケット入力UI（UIToolkit）
│   ├── MelpomeneTicketView.cs   # チケット表示UI
│   └── MelpomeneInputView.uxml  # UIToolkitレイアウト
└── Gizmos/
    └── MelpomeneGizmoDrawer.cs  # シーン上のチケット表示
```

---

# チケット発行フロー

## 1. 入力UI表示
- **トリガー**: GameView/SceneView上でAlt+クリック
- **処理**:
  1. クリック位置のスクリーン座標を取得
  2. Raycastで対象オブジェクトを特定（あれば）
  3. MelpomeneInputViewを表示
  4. 初期値を自動入力（シーン名、オブジェクト名、座標）

## 2. チケット情報入力
### 必須入力項目
| 項目 | 型 | 説明 |
|------|-----|------|
| userName | string | 報告者のユーザー名 |
| title | string | チケットタイトル（概要） |
| description | string | 詳細説明 |

### 自動取得項目
| 項目 | 型 | 説明 |
|------|-----|------|
| sceneName | string | 現在のシーン名 |
| targetObjectPath | string | 対象オブジェクトのHierarchyパス |
| screenPosition | Vector2 | クリック時のスクリーン座標 |
| worldPosition | Vector3 | 対象オブジェクトのワールド座標（あれば） |
| timestamp | DateTime | 作成日時 |

### インスペクタ追加入力（任意）
- priority: 優先度（Low/Medium/High/Critical）
- category: カテゴリ（Bug/Feature/Improvement/Question）
- labels: GitHubラベル（カンマ区切り）

## 3. GitHub送信
- **トリガー**: 送信ボタンクリック
- **処理**:
  1. 入力値のバリデーション
  2. GitHub Issues APIでIssue作成
  3. 成功時: UIを閉じ、ローカルキャッシュを更新
  4. 失敗時: エラーメッセージ表示

---

# チケット取得フロー

## 1. 初期化時のキャッシュ取得
- **タイミング**: エディタ起動時、またはMelpomeneWindow初回表示時
- **処理**:
  1. GitHub Issues APIでOpen状態のIssueを取得
  2. Melpomeneタグ（`[Melpomene]`）を含むIssueをフィルタ
  3. ローカルキャッシュに保存
  4. キャッシュ有効期限: 10分

## 2. シーン対応チケット表示
- **処理**:
  1. 現在のシーン名でキャッシュをフィルタ
  2. 各チケットの対象オブジェクトの可視状態をチェック
  3. 可視オブジェクトのチケットのみGizmoで表示
  4. チケットアイコンをクリックで詳細表示

---

# GitHub Issue フォーマット

## Issueタイトル
```
[Melpomene] {title}
```

## Issue本文
```markdown
## 報告者
{userName}

## シーン情報
- **シーン**: {sceneName}
- **オブジェクト**: {targetObjectPath}
- **スクリーン座標**: ({screenPosition.x}, {screenPosition.y})
- **ワールド座標**: ({worldPosition.x}, {worldPosition.y}, {worldPosition.z})

## 説明
{description}

## メタデータ
- **優先度**: {priority}
- **カテゴリ**: {category}
- **作成日時**: {timestamp}

---
*このIssueはMelpomeneによって自動生成されました*
```

---

# クラス設計

## MelpomeneManager
シングルトン。ツール全体の管理を行う。

### プロパティ
- Instance: シングルトンインスタンス
- Config: MelpomeneConfig参照
- Cache: MelpomeneCacheインスタンス
- GitHubClient: MelpomeneGitHubClientインスタンス

### メソッド
- Initialize(): 初期化、キャッシュ取得
- CreateTicket(MelpomeneTicket): チケット作成
- GetTicketsForScene(string sceneName): シーン別チケット取得
- RefreshCache(): キャッシュ更新

## MelpomeneTicket
チケットデータを表すクラス。

### プロパティ
- issueNumber: int（GitHub Issue番号）
- userName: string
- title: string
- description: string
- sceneName: string
- targetObjectPath: string
- screenPosition: Vector2
- worldPosition: Vector3
- priority: MelpomenePriority
- category: MelpomeneCategory
- timestamp: DateTime
- issueUrl: string

## MelpomeneGitHubClient
GitHub APIとの通信を行う。

### メソッド
- CreateIssueAsync(MelpomeneTicket): Issue作成
- GetIssuesAsync(): Issue一覧取得
- CloseIssueAsync(int issueNumber): Issueクローズ

## MelpomeneCache
チケットのローカルキャッシュを管理。

### プロパティ
- CacheDuration: TimeSpan（10分）
- LastFetchTime: DateTime
- Tickets: List<MelpomeneTicket>

### メソッド
- IsExpired(): キャッシュ有効期限チェック
- Update(List<MelpomeneTicket>): キャッシュ更新
- GetByScene(string sceneName): シーン別取得

## MelpomeneConfig (ScriptableObject)
設定を保持するアセット。

### プロパティ
- repositoryOwner: string（リポジトリオーナー）
- repositoryName: string（リポジトリ名）
- accessToken: string（GitHub Personal Access Token）
- defaultLabels: string[]（デフォルトラベル）
- cacheDurationMinutes: int（キャッシュ有効期限）

---

# 依存関係
- Unity 6000.0.49f1+
- UIToolkit
- UnityWebRequest（GitHub API通信）
- Editor専用（#if UNITY_EDITOR）

# セキュリティ注意
- accessTokenは.gitignoreに追加されたファイルで管理すること
- MelpomeneConfig.assetはGit管理外とするか、トークンを別ファイルに分離
