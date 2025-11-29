import { Octokit } from "octokit";
import dotenv from "dotenv";
import * as fs from "fs/promises";
import path from "path";

// .envファイルから環境変数を読み込む
dotenv.config();

// --- 定数定義 ---
const OUTPUT_DIR = "../tasktemp";

// --- 設定値の読み込みと検証 ---
const { GITHUB_TOKEN, OWNER, REPO } = process.env;
const PULL_REQUEST_NUMBER = process.argv[2];

if (!GITHUB_TOKEN || !OWNER || !REPO) {
  console.error("エラー: 必要な環境変数 (GITHUB_TOKEN, OWNER, REPO) が.envファイルに設定されていません。");
  process.exit(1);
}

if (!PULL_REQUEST_NUMBER) {
  console.error("エラー: コマンドライン引数としてプルリクエスト番号が指定されていません。");
  console.error("使用法: node index.js <プルリクエスト番号>");
  process.exit(1);
}

const octokit = new Octokit({ auth: GITHUB_TOKEN });

// --- GraphQLクエリの定義 --- (変更なし)
const PULL_REQUEST_REVIEWS_QUERY = `
  query($owner: String!, $repo: String!, $pullRequestNumber: Int!, $threadsCursor: String) {
    repository(owner: $owner, name:$repo) {
      pullRequest(number: $pullRequestNumber) {
        reviewThreads(first: 100, after: $threadsCursor) {
          pageInfo {
            endCursor
            hasNextPage
          }
          nodes {
            id
            isResolved
            comments(first: 100) {
              nodes {
                author {
                  login
                }
                databaseId
                body
                url
                createdAt
              }
            }
          }
        }
      }
    }
  }
`;

/**
 * PRに紐づく全てのレビューコメントスレッドを取得する関数 (変更なし)
 */
async function fetchAllReviewThreads() {
    let allThreads = [];
    let hasNextPage = true;
    let cursor = null;
    let pageCount = 1;

    console.log(`[INFO] ${OWNER}/${REPO}#${PULL_REQUEST_NUMBER} のレビュー取得を開始します...`);

    while (hasNextPage) {
        try {
            console.log(`[INFO] スレッドのページ ${pageCount} を取得中...`);
            const response = await octokit.graphql(PULL_REQUEST_REVIEWS_QUERY, {
                owner: OWNER,
                repo: REPO,
                pullRequestNumber: parseInt(PULL_REQUEST_NUMBER, 10),
                threadsCursor: cursor,
            });
            const reviewThreads = response.repository.pullRequest.reviewThreads;
            if (reviewThreads.nodes && reviewThreads.nodes.length > 0) {
                allThreads = allThreads.concat(reviewThreads.nodes);
            }
            hasNextPage = reviewThreads.pageInfo.hasNextPage;
            cursor = reviewThreads.pageInfo.endCursor;
            pageCount++;
        } catch (error) {
            console.error("[ERROR] GraphQLクエリの実行中にエラーが発生しました:", error.message);
            hasNextPage = false;
        }
    }
    console.log(`[INFO] 全 ${allThreads.length} 件のレビュースレッドを取得しました。`);
    return allThreads;
}

/**
 * タスクファイルを生成する関数 (変更なし)
 */
async function createTaskFile(comment, taskNumber) {
  const markdownContent = `
# PR修正 タスク指示書

## コメントID
${comment.databaseId}

## URL
${comment.url}

## 修正内容
${comment.body}
`.trim();

  const fileName = `task_${PULL_REQUEST_NUMBER}_${taskNumber}.md`;
  const filePath = path.join(OUTPUT_DIR, fileName);

  try {
    await fs.writeFile(filePath, markdownContent, "utf8");
    console.log(`[SUCCESS] タスクファイルを作成しました: ${filePath}`);
  } catch (error) {
    console.error(`[ERROR] ファイルの書き込みに失敗しました: ${filePath}`, error);
  }
}

/**
 * メイン処理 (最新コメントの選別ロジックを追加)
 */
async function main() {
  // 出力用ディレクトリを準備
  try {
    await fs.rm(OUTPUT_DIR, { recursive: true, force: true });
    await fs.mkdir(OUTPUT_DIR);
    console.log(`[INFO] 出力先ディレクトリ '${OUTPUT_DIR}' を準備しました。`);
  } catch (error) {
    console.error(`[ERROR] ディレクトリの準備に失敗しました:`, error);
    process.exit(1);
  }

  const allThreads = await fetchAllReviewThreads();

  if (allThreads.length > 0) {
    const unresolvedThreads = allThreads.filter(thread => !thread.isResolved);
    console.log(`[INFO] 未解決のスレッドは ${unresolvedThreads.length} 件です。`);

    let taskCounter = 1;

    // ----- ここからが変更箇所 -----
    // 未解決スレッドごとに処理
    for (const thread of unresolvedThreads) {
      const comments = thread.comments?.nodes;

      // スレッドにコメントが存在しない場合はスキップ
      if (!comments || comments.length === 0) {
        continue;
      }

      // 'createdAt' (作成日時) を基準にコメントをソートし、最新のものを取得
      // Dateオブジェクトに変換して比較することで、正確な時系列順にします
      const latestComment = [...comments].sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt))[0];

      // 最新のコメント1件だけでタスクファイルを生成
      await createTaskFile(latestComment, taskCounter);
      taskCounter++;
    }
    // ----- ここまでが変更箇所 -----

    console.log(`\n[COMPLETE] 処理が完了しました。合計 ${taskCounter - 1} 件のタスクファイルを '${OUTPUT_DIR}' に出力しました。`);
  }
}

// 実行
main();