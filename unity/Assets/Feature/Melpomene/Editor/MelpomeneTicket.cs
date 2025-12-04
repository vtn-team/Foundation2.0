#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Melpomene
{
    /// <summary>
    /// チケットの優先度
    /// </summary>
    public enum MelpomenePriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// チケットのカテゴリ
    /// </summary>
    public enum MelpomeneCategory
    {
        Bug,
        Feature,
        Improvement,
        Question
    }

    /// <summary>
    /// Melpomeneチケットデータ
    /// NOTE: GitHub Issueと対応するチケット情報を保持
    /// </summary>
    [Serializable]
    public class MelpomeneTicket
    {
        /// <summary>GitHub Issue番号（未作成時は-1）</summary>
        public int issueNumber = -1;

        /// <summary>報告者のユーザー名</summary>
        public string userName;

        /// <summary>チケットタイトル（概要）</summary>
        public string title;

        /// <summary>詳細説明</summary>
        public string description;

        /// <summary>シーン名</summary>
        public string sceneName;

        /// <summary>対象オブジェクトのHierarchyパス</summary>
        public string targetObjectPath;

        /// <summary>クリック時のスクリーン座標</summary>
        public Vector2 screenPosition;

        /// <summary>対象オブジェクトのワールド座標</summary>
        public Vector3 worldPosition;

        /// <summary>優先度</summary>
        public MelpomenePriority priority = MelpomenePriority.Medium;

        /// <summary>カテゴリ</summary>
        public MelpomeneCategory category = MelpomeneCategory.Bug;

        /// <summary>追加ラベル（カンマ区切り）</summary>
        public string labels;

        /// <summary>作成日時</summary>
        public string timestamp;

        /// <summary>GitHub Issue URL</summary>
        public string issueUrl;

        /// <summary>Issueの状態（open/closed）</summary>
        public string state = "open";

        /// <summary>
        /// GitHub Issue本文を生成する
        /// </summary>
        public string GenerateIssueBody()
        {
            return $@"## 報告者
{userName}

## シーン情報
- **シーン**: {sceneName}
- **オブジェクト**: {(string.IsNullOrEmpty(targetObjectPath) ? "(なし)" : targetObjectPath)}
- **スクリーン座標**: ({screenPosition.x:F0}, {screenPosition.y:F0})
- **ワールド座標**: ({worldPosition.x:F2}, {worldPosition.y:F2}, {worldPosition.z:F2})

## 説明
{description}

## メタデータ
- **優先度**: {priority}
- **カテゴリ**: {category}
- **作成日時**: {timestamp}

---
*このIssueはMelpomeneによって自動生成されました*
<sub>Melpomene v{MelpomeneConfig.Version}</sub>";
        }

        /// <summary>
        /// GitHub Issueタイトルを生成する
        /// </summary>
        public string GenerateIssueTitle()
        {
            return $"[Melpomene] {title}";
        }

        /// <summary>
        /// GitHub Issue本文からチケット情報をパースする
        /// </summary>
        public static MelpomeneTicket ParseFromIssue(int number, string issueTitle, string issueBody, string url, string issueState, string createdAt, string[] labels)
        {
            var ticket = new MelpomeneTicket
            {
                issueNumber = number,
                issueUrl = url,
                state = issueState
            };

            // タイトルから[Melpomene]を除去
            if (issueTitle.StartsWith("[Melpomene] "))
            {
                ticket.title = issueTitle.Substring(12);
            }
            else
            {
                ticket.title = issueTitle;
            }

            // ラベルから優先度とカテゴリを取得
            foreach (var label in labels)
            {
                var lowerLabel = label.ToLower();
                // 優先度
                if (Enum.TryParse<MelpomenePriority>(label, true, out var priority))
                {
                    ticket.priority = priority;
                }
                // カテゴリ
                if (Enum.TryParse<MelpomeneCategory>(label, true, out var category))
                {
                    ticket.category = category;
                }
            }

            // 作成日時（GitHub APIからの値を使用）
            if (!string.IsNullOrEmpty(createdAt))
            {
                // ISO 8601形式をパース: "2025-01-15T10:30:00Z"
                if (DateTime.TryParse(createdAt, out DateTime dt))
                {
                    ticket.timestamp = dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    ticket.timestamp = createdAt;
                }
            }

            // 本文をパース
            try
            {
                var lines = issueBody.Split('\n');
                string currentSection = "";

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();

                    if (trimmedLine.StartsWith("## "))
                    {
                        currentSection = trimmedLine.Substring(3);
                        continue;
                    }

                    if (currentSection == "報告者" && !string.IsNullOrEmpty(trimmedLine))
                    {
                        ticket.userName = trimmedLine;
                    }
                    else if (TryExtractValue(trimmedLine, "- **シーン**:", out var sceneName))
                    {
                        ticket.sceneName = sceneName;
                    }
                    else if (TryExtractValue(trimmedLine, "- **オブジェクト**:", out var objPath))
                    {
                        ticket.targetObjectPath = objPath == "(なし)" ? "" : objPath;
                    }
                    else if (TryExtractValue(trimmedLine, "- **スクリーン座標**:", out var screenCoords))
                    {
                        var coords = screenCoords.Trim('(', ')').Split(',');
                        if (coords.Length >= 2)
                        {
                            float.TryParse(coords[0].Trim(), out float x);
                            float.TryParse(coords[1].Trim(), out float y);
                            ticket.screenPosition = new Vector2(x, y);
                        }
                    }
                    else if (TryExtractValue(trimmedLine, "- **ワールド座標**:", out var worldCoords))
                    {
                        var coords = worldCoords.Trim('(', ')').Split(',');
                        if (coords.Length >= 3)
                        {
                            float.TryParse(coords[0].Trim(), out float x);
                            float.TryParse(coords[1].Trim(), out float y);
                            float.TryParse(coords[2].Trim(), out float z);
                            ticket.worldPosition = new Vector3(x, y, z);
                        }
                    }
                    else if (currentSection == "説明" && !trimmedLine.StartsWith("##") && !trimmedLine.StartsWith("---"))
                    {
                        if (!string.IsNullOrEmpty(ticket.description))
                        {
                            ticket.description += "\n";
                        }
                        ticket.description += trimmedLine;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Melpomene] Failed to parse issue body: {e.Message}");
            }

            return ticket;
        }

        /// <summary>
        /// マークダウンの行から値を抽出する
        /// </summary>
        private static bool TryExtractValue(string line, string prefix, out string value)
        {
            value = "";
            if (line.StartsWith(prefix))
            {
                value = line.Substring(prefix.Length).Trim();
                return true;
            }
            return false;
        }
    }
}
#endif
