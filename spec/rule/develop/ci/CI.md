
# CIルール
CIルールを記載する。


# CI環境
- GitHub Actions
	- Self-Hosted Runnerで、すでに構築された誰かのマシン環境上でチェックを行う


# GitHub Secrets設定
CIを動作させるために以下のSecretsを設定する必要がある。


## 必須設定

| Secret名             | 説明                 | 例                                                                |
| ------------------- | ------------------ | ---------------------------------------------------------------- |
| `UNITY_EDITOR_PATH` | Unityエディタの実行ファイルパス | `C:\Program Files\Unity\Hub\Editor\6000.0.49f1\Editor\Unity.exe` |
| `BUILD_OUTPUT_PATH` | ビルド成果物の出力先パス       | `C:\Artifacts`                                                   |

## オプション設定（Slack通知用）

| Secret名            | 説明              | 取得方法             |
| ------------------ | --------------- | ---------------- |
| `SLACK_BOT_TOKEN`  | Slack Bot Token | Slack App設定から取得  |
| `SLACK_CHANNEL_ID` | 投稿先チャンネルID      | Slackチャンネル設定から取得 |

### Secrets設定手順

1. GitHubリポジトリの「Settings」を開く
2. 「Secrets and variables」→「Actions」を選択
3. 「New repository secret」をクリック
4. Secret名と値を入力して保存


# Slack通知設定

## Slack Bot Tokenの取得方法

1. [Slack API](https://api.slack.com/apps)にアクセス
2. 対象のSlack Appを選択（または新規作成）
3. 「OAuth & Permissions」を開く
4. 「Bot Token Scopes」に以下を追加:
   - `chat:write` - メッセージ投稿用
5. 「Install to Workspace」でアプリをインストール
6. 「Bot User OAuth Token」（xoxb-から始まる）をコピー
7. GitHubのSecretsに`SLACK_BOT_TOKEN`として登録

## チャンネルIDの取得方法

1. Slackで対象チャンネルを開く
2. チャンネル名をクリック → 「チャンネル詳細を開く」
3. 画面下部の「チャンネルID」をコピー
4. GitHubのSecretsに`SLACK_CHANNEL_ID`として登録

**注意**: Botは事前にチャンネルに招待済みであること（`/invite @BotName`）


# 運用とCIの実装

## Unity ビルド
タイミング： 定期実行（毎日JST 6:00）、手動実行
ワークフロー: `.github/workflows/unity-build.yml`

### 処理内容
1. リポジトリをチェックアウト
2. GitHub SecretsからUnityエディタパス・出力先パスを取得・検証
3. ビルド出力ディレクトリを準備
4. Unityプロジェクトをビルド
	- BuildScript.PerformBuild を実行
	- Development/Release プロファイルを選択可能
5. ビルド結果を確認
	- 成功時:
		- ビルド成果物をZIPファイルに圧縮（`Build_{Profile}_{RunNumber}_{Timestamp}.zip`）
		- ZIPファイルをBUILD_OUTPUT_PATHで指定されたパスに移動
		- 成功通知をSlackに送信
	- 失敗時: エラーログを抽出、失敗通知をSlackに送信
6. ビルドログをアーティファクトとしてアップロード

### 手動実行
1. GitHubリポジトリの「Actions」タブを開く
2. 「Unity Build」ワークフローを選択
3. 「Run workflow」をクリック
4. ビルドプロファイル（Development/Release）を選択
5. 「Run workflow」で実行


## 日次実装レビュー
タイミング： 定期実行（毎日JST 4:00）、手動実行
ワークフロー: `.github/workflows/daily-review.yml`

### 処理内容
1. 前回実行時刻を取得（初回は記録のみ）
2. 前回以降の変更を収集
	- mainブランチの新しいコミット
	- 更新されたPR一覧（Open/Closed状態含む）
3. サマリーを作成してSlackに投稿

### 手動実行
1. GitHubリポジトリの「Actions」タブを開く
2. 「Daily Implementation Review」ワークフローを選択
3. 「Run workflow」をクリック


## PRコンパイルチェック
タイミング： PR作成時、PR更新時
ワークフロー: `.github/workflows/pr-compile-check.yml`

### 処理内容

1. リポジトリをチェックアウト
2. GitHub SecretsからUnityエディタパスを取得・検証
3. Unityプロジェクトのコンパイルチェック
	- BuildScript.CompilePlayerScripts を実行
4. 結果をPRにコメント
	- 成功時: 成功メッセージをコメント
	- 失敗時: エラー詳細をコメント


# Unity BuildScript
CIで実行するUnityメソッドは全て `BuildScript` クラス（`unity/Assets/Scripts/Editor/BuildScript.cs`）に集約する。

## メソッド一覧

| メソッド                               | 説明             | 使用ワークフロー             |
| ---------------------------------- | -------------- | -------------------- |
| `BuildScript.PerformBuild`         | プレイヤービルドを実行    | unity-build.yml      |
| `BuildScript.CompilePlayerScripts` | スクリプトコンパイルチェック | pr-compile-check.yml |

## コマンドライン引数

### PerformBuild
```bash
Unity.exe -batchmode -quit -projectPath . -executeMethod BuildScript.PerformBuild -buildProfile Development -buildPath "C:\Build\Development" -logFile build.log
```

| 引数              | 説明                             | デフォルト             |
| --------------- | ------------------------------ | ----------------- |
| `-buildProfile` | ビルドプロファイル（Development/Release） | Development       |
| `-buildPath`    | ビルド出力先パス                       | Build/Development |

### CompilePlayerScripts
```bash
Unity.exe -batchmode -quit -projectPath . -executeMethod BuildScript.CompilePlayerScripts -logFile compile.log
```

引数なし。コンパイルエラーがある場合は終了コード1で終了。


# スクリプト一覧

| スクリプト                    | 説明                             |
| ------------------------ | ------------------------------ |
| `script/slack-notify.js` | Slack Bot通知送信（postMessage API） |

## slack-notify.js
Slack Bot Token + chat.postMessage APIを使用して各種通知を送信する。

### 使用方法

```bash
# ビルド成功通知
node script/slack-notify.js success

# ビルド失敗通知（エラーファイル指定）
node script/slack-notify.js failure build-errors.txt

# 日次レビュー通知（サマリーファイル指定）
node script/slack-notify.js daily-review summary.txt
```

### 環境変数

| 変数名                 | 必須  | 説明                          |
| ------------------- | --- | --------------------------- |
| `SLACK_BOT_TOKEN`   | ○   | Slack Bot Token（xoxb-から始まる） |
| `SLACK_CHANNEL_ID`  | ○   | 投稿先チャンネルID                  |
| `GITHUB_REPOSITORY` |     | リポジトリ名                      |
| `GITHUB_RUN_ID`     |     | Actions実行ID                 |
| `GITHUB_RUN_NUMBER` |     | ビルド番号                       |
| `BUILD_PROFILE`     |     | ビルドプロファイル名                  |
| `BUILD_EXIT_CODE`   |     | 終了コード（失敗時）                  |


# セキュリティ注意事項
- **Secretsはログに出力しない**: GitHub Actionsは自動的にマスクするが、独自スクリプトでも注意
- **Bot Tokenは公開しない**: 漏洩すると第三者がBotとしてメッセージを送信できてしまう
- **ローカル実行は非推奨**: このCI設計はGitHub Actions専用。ローカル環境での実行は考慮していない
