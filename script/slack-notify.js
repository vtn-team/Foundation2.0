#!/usr/bin/env node

/**
 * Slack通知スクリプト
 * Slack Bot Token + chat.postMessage APIを使用して各種通知を送信
 *
 * 使用方法:
 *   node slack-notify.js <type> [file_path]
 *
 * タイプ:
 *   success      - ビルド成功通知
 *   failure      - ビルド失敗通知（file_path: エラーログファイル）
 *   daily-review - 日次レビュー通知（file_path: サマリーファイル）
 *
 * 環境変数:
 *   SLACK_BOT_TOKEN    - Slack Bot Token（xoxb-から始まる、必須）
 *   SLACK_CHANNEL_ID   - 投稿先チャンネルID（必須）
 *   GITHUB_REPOSITORY  - GitHubリポジトリ名（例: owner/repo）
 *   GITHUB_RUN_ID      - GitHub Actions実行ID
 *   GITHUB_RUN_NUMBER  - GitHub Actions実行番号
 *   BUILD_PROFILE      - ビルドプロファイル名（ビルド通知用）
 *   BUILD_EXIT_CODE    - ビルド終了コード（failure用）
 */

const https = require('https');
const fs = require('fs');

/**
 * Slack chat.postMessage APIにメッセージを送信
 * @param {string} botToken - Slack Bot Token
 * @param {string} channelId - チャンネルID
 * @param {object} payload - Slackメッセージペイロード（blocks, attachments等）
 * @returns {Promise<object>}
 */
function sendSlackMessage(botToken, channelId, payload) {
    return new Promise((resolve, reject) => {
        const data = JSON.stringify({
            channel: channelId,
            ...payload
        });

        const options = {
            hostname: 'slack.com',
            port: 443,
            path: '/api/chat.postMessage',
            method: 'POST',
            headers: {
                'Content-Type': 'application/json; charset=utf-8',
                'Authorization': `Bearer ${botToken}`,
                'Content-Length': Buffer.byteLength(data)
            }
        };

        const req = https.request(options, (res) => {
            let responseData = '';
            res.on('data', (chunk) => {
                responseData += chunk;
            });
            res.on('end', () => {
                try {
                    const result = JSON.parse(responseData);
                    if (result.ok) {
                        resolve(result);
                    } else {
                        reject(new Error(`Slack API error: ${result.error}`));
                    }
                } catch (e) {
                    reject(new Error(`Failed to parse response: ${responseData}`));
                }
            });
        });

        req.on('error', (error) => {
            reject(error);
        });

        req.write(data);
        req.end();
    });
}

/**
 * 成功時のSlackメッセージを作成
 * @returns {object} Slackペイロード
 */
function createSuccessMessage() {
    const repository = process.env.GITHUB_REPOSITORY || 'unknown/repo';
    const runId = process.env.GITHUB_RUN_ID || '';
    const runNumber = process.env.GITHUB_RUN_NUMBER || '';
    const buildProfile = process.env.BUILD_PROFILE || 'Development';

    const actionsUrl = `https://github.com/${repository}/actions/runs/${runId}`;

    return {
        attachments: [
            {
                color: '#36a64f', // 緑色
                blocks: [
                    {
                        type: 'header',
                        text: {
                            type: 'plain_text',
                            text: '✅ Unity ビルド成功',
                            emoji: true
                        }
                    },
                    {
                        type: 'section',
                        fields: [
                            {
                                type: 'mrkdwn',
                                text: `*リポジトリ:*\n${repository}`
                            },
                            {
                                type: 'mrkdwn',
                                text: `*ビルド番号:*\n#${runNumber}`
                            },
                            {
                                type: 'mrkdwn',
                                text: `*プロファイル:*\n${buildProfile}`
                            }
                        ]
                    },
                    {
                        type: 'actions',
                        elements: [
                            {
                                type: 'button',
                                text: {
                                    type: 'plain_text',
                                    text: 'Actions を確認',
                                    emoji: true
                                },
                                url: actionsUrl
                            }
                        ]
                    }
                ]
            }
        ]
    };
}

/**
 * 失敗時のSlackメッセージを作成
 * @param {string} errorContent - エラーログの内容
 * @returns {object} Slackペイロード
 */
function createFailureMessage(errorContent) {
    const repository = process.env.GITHUB_REPOSITORY || 'unknown/repo';
    const runId = process.env.GITHUB_RUN_ID || '';
    const runNumber = process.env.GITHUB_RUN_NUMBER || '';
    const buildProfile = process.env.BUILD_PROFILE || 'Development';
    const exitCode = process.env.BUILD_EXIT_CODE || 'unknown';

    const actionsUrl = `https://github.com/${repository}/actions/runs/${runId}`;

    // エラー内容を最大1500文字に制限（Slackの制限対策）
    const truncatedError = errorContent.length > 1500
        ? errorContent.substring(0, 1500) + '\n... (省略)'
        : errorContent;

    return {
        attachments: [
            {
                color: '#dc3545', // 赤色
                blocks: [
                    {
                        type: 'header',
                        text: {
                            type: 'plain_text',
                            text: '❌ Unity ビルド失敗',
                            emoji: true
                        }
                    },
                    {
                        type: 'section',
                        fields: [
                            {
                                type: 'mrkdwn',
                                text: `*リポジトリ:*\n${repository}`
                            },
                            {
                                type: 'mrkdwn',
                                text: `*ビルド番号:*\n#${runNumber}`
                            },
                            {
                                type: 'mrkdwn',
                                text: `*プロファイル:*\n${buildProfile}`
                            },
                            {
                                type: 'mrkdwn',
                                text: `*終了コード:*\n${exitCode}`
                            }
                        ]
                    },
                    {
                        type: 'section',
                        text: {
                            type: 'mrkdwn',
                            text: `*エラー詳細:*\n\`\`\`${truncatedError}\`\`\``
                        }
                    },
                    {
                        type: 'actions',
                        elements: [
                            {
                                type: 'button',
                                text: {
                                    type: 'plain_text',
                                    text: 'Actions を確認',
                                    emoji: true
                                },
                                url: actionsUrl,
                                style: 'danger'
                            }
                        ]
                    }
                ]
            }
        ]
    };
}

/**
 * 日次レビュー通知のSlackメッセージを作成
 * @param {string} summaryContent - サマリーの内容
 * @returns {object} Slackペイロード
 */
function createDailyReviewMessage(summaryContent) {
    const repository = process.env.GITHUB_REPOSITORY || 'unknown/repo';
    const today = new Date().toLocaleDateString('ja-JP', {
        year: 'numeric',
        month: 'long',
        day: 'numeric'
    });

    const repoUrl = `https://github.com/${repository}`;

    // サマリー内容を最大2500文字に制限
    const truncatedSummary = summaryContent.length > 2500
        ? summaryContent.substring(0, 2500) + '\n... (省略)'
        : summaryContent;

    return {
        attachments: [
            {
                color: '#0066cc', // 青色
                blocks: [
                    {
                        type: 'header',
                        text: {
                            type: 'plain_text',
                            text: '📋 日次実装レビュー',
                            emoji: true
                        }
                    },
                    {
                        type: 'context',
                        elements: [
                            {
                                type: 'mrkdwn',
                                text: `📅 ${today} | 📁 ${repository}`
                            }
                        ]
                    },
                    {
                        type: 'divider'
                    },
                    {
                        type: 'section',
                        text: {
                            type: 'mrkdwn',
                            text: truncatedSummary
                        }
                    },
                    {
                        type: 'divider'
                    },
                    {
                        type: 'actions',
                        elements: [
                            {
                                type: 'button',
                                text: {
                                    type: 'plain_text',
                                    text: 'リポジトリを確認',
                                    emoji: true
                                },
                                url: repoUrl
                            },
                            {
                                type: 'button',
                                text: {
                                    type: 'plain_text',
                                    text: 'PRを確認',
                                    emoji: true
                                },
                                url: `${repoUrl}/pulls`
                            }
                        ]
                    }
                ]
            }
        ]
    };
}

/**
 * メイン処理
 */
async function main() {
    const args = process.argv.slice(2);

    if (args.length < 1) {
        console.error('使用方法: node slack-notify.js <type> [file_path]');
        console.error('  type: success, failure, daily-review');
        process.exit(1);
    }

    const notifyType = args[0].toLowerCase();
    const filePath = args[1];

    // Bot TokenとChannel IDを取得
    const botToken = process.env.SLACK_BOT_TOKEN;
    const channelId = process.env.SLACK_CHANNEL_ID;

    if (!botToken) {
        console.error('❌ SLACK_BOT_TOKEN 環境変数が設定されていません');
        process.exit(1);
    }

    if (!channelId) {
        console.error('❌ SLACK_CHANNEL_ID 環境変数が設定されていません');
        process.exit(1);
    }

    let payload;

    if (notifyType === 'success') {
        payload = createSuccessMessage();
        console.log('✅ ビルド成功通知を送信中...');
    } else if (notifyType === 'failure') {
        // エラーファイルの内容を読み込む
        let errorContent = 'エラー詳細は Actions ログを確認してください。';
        if (filePath && fs.existsSync(filePath)) {
            try {
                errorContent = fs.readFileSync(filePath, 'utf8');
            } catch (e) {
                console.warn(`⚠️ エラーファイルの読み込みに失敗: ${e.message}`);
            }
        }
        payload = createFailureMessage(errorContent);
        console.log('❌ ビルド失敗通知を送信中...');
    } else if (notifyType === 'daily-review') {
        // サマリーファイルの内容を読み込む
        let summaryContent = '変更内容はリポジトリを確認してください。';
        if (filePath && fs.existsSync(filePath)) {
            try {
                summaryContent = fs.readFileSync(filePath, 'utf8');
            } catch (e) {
                console.warn(`⚠️ サマリーファイルの読み込みに失敗: ${e.message}`);
            }
        }
        payload = createDailyReviewMessage(summaryContent);
        console.log('📋 日次レビュー通知を送信中...');
    } else {
        console.error(`❌ 無効な通知タイプ: ${notifyType}`);
        console.error('  有効な値: success, failure, daily-review');
        process.exit(1);
    }

    try {
        await sendSlackMessage(botToken, channelId, payload);
        console.log('✅ Slack通知を送信しました');
    } catch (error) {
        console.error(`❌ Slack通知の送信に失敗: ${error.message}`);
        process.exit(1);
    }
}

// スクリプトが直接実行された場合のみメイン処理を実行
if (require.main === module) {
    main();
}

module.exports = { sendSlackMessage, createSuccessMessage, createFailureMessage, createDailyReviewMessage };
