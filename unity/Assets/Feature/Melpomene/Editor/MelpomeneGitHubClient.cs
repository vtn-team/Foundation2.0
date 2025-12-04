#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

namespace Melpomene
{
    /// <summary>
    /// GitHub API クライアント
    /// NOTE: GitHub Issues APIとの通信を担当
    /// </summary>
    public class MelpomeneGitHubClient
    {
        private readonly MelpomeneConfig config;

        public MelpomeneGitHubClient(MelpomeneConfig config)
        {
            this.config = config;
        }

        /// <summary>
        /// Issueを作成する
        /// </summary>
        public async UniTask<MelpomeneTicket> CreateIssueAsync(MelpomeneTicket ticket)
        {
            if (!config.IsValid)
            {
                Debug.LogError("[Melpomene] Config is not valid. Please set repository and access token.");
                return null;
            }

            var url = $"{config.ApiBaseUrl}/issues";

            // ラベルを構築
            var labels = new List<string>(config.defaultLabels);
            if (!string.IsNullOrEmpty(ticket.labels))
            {
                labels.AddRange(ticket.labels.Split(','));
            }
            labels.Add(ticket.priority.ToString().ToLower());
            labels.Add(ticket.category.ToString().ToLower());

            // リクエストボディを構築
            var requestBody = new GitHubIssueRequest
            {
                title = ticket.GenerateIssueTitle(),
                body = ticket.GenerateIssueBody(),
                labels = labels.ToArray()
            };

            var json = JsonUtility.ToJson(requestBody);
            var bodyBytes = Encoding.UTF8.GetBytes(json);

            using (var request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyBytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {config.accessToken}");
                request.SetRequestHeader("Accept", "application/vnd.github+json");
                request.SetRequestHeader("User-Agent", "Melpomene-Unity");

                try
                {
                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var response = JsonUtility.FromJson<GitHubIssueResponse>(request.downloadHandler.text);
                        ticket.issueNumber = response.number;
                        ticket.issueUrl = response.html_url;
                        ticket.state = response.state;
                        Debug.Log($"[Melpomene] Issue created: #{response.number} - {response.html_url}");
                        return ticket;
                    }
                    else
                    {
                        Debug.LogError($"[Melpomene] Failed to create issue: {request.error}\n{request.downloadHandler.text}");
                        return null;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Melpomene] Exception creating issue: {e.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// Melpomeneタグ付きのIssue一覧を取得する
        /// </summary>
        public async UniTask<List<MelpomeneTicket>> GetIssuesAsync()
        {
            if (!config.IsValid)
            {
                Debug.LogError("[Melpomene] Config is not valid. Please set repository and access token.");
                return new List<MelpomeneTicket>();
            }

            var tickets = new List<MelpomeneTicket>();
            // NOTE: ラベルフィルタを外し、全てのopen issueを取得
            var url = $"{config.ApiBaseUrl}/issues?state=open&per_page=100";

            Debug.Log($"[Melpomene] Fetching issues from: {url}");

            using (var request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", $"Bearer {config.accessToken}");
                request.SetRequestHeader("Accept", "application/vnd.github+json");
                request.SetRequestHeader("User-Agent", "Melpomene-Unity");

                try
                {
                    await request.SendWebRequest();

                    Debug.Log($"[Melpomene] Response code: {request.responseCode}");

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        // JSONの配列をパース
                        var jsonArray = request.downloadHandler.text;
                        Debug.Log($"[Melpomene] Response length: {jsonArray.Length} chars");

                        var issues = ParseIssueArray(jsonArray);
                        Debug.Log($"[Melpomene] Parsed {issues.Count} issues from response");

                        foreach (var issueData in issues)
                        {
                            // [Melpomene]タグを含むIssueのみ処理
                            if (issueData.issue.title != null && issueData.issue.title.Contains("[Melpomene]"))
                            {
                                var ticket = MelpomeneTicket.ParseFromIssue(
                                    issueData.issue.number,
                                    issueData.issue.title,
                                    issueData.issue.body ?? "",
                                    issueData.issue.html_url,
                                    issueData.issue.state,
                                    issueData.issue.created_at,
                                    issueData.labels
                                );
                                tickets.Add(ticket);
                                Debug.Log($"[Melpomene] Added ticket: #{issueData.issue.number} - {issueData.issue.title} (labels: {string.Join(", ", issueData.labels)})");
                            }
                        }

                        Debug.Log($"[Melpomene] Fetched {tickets.Count} Melpomene tickets from GitHub (total issues: {issues.Count})");
                    }
                    else
                    {
                        Debug.LogError($"[Melpomene] Failed to fetch issues: {request.error}\nResponse: {request.downloadHandler.text}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Melpomene] Exception fetching issues: {e.Message}\n{e.StackTrace}");
                }
            }

            return tickets;
        }

        /// <summary>
        /// Issueをクローズする
        /// </summary>
        public async UniTask<bool> CloseIssueAsync(int issueNumber)
        {
            if (!config.IsValid)
            {
                Debug.LogError("[Melpomene] Config is not valid.");
                return false;
            }

            var url = $"{config.ApiBaseUrl}/issues/{issueNumber}";
            var json = "{\"state\":\"closed\"}";
            var bodyBytes = Encoding.UTF8.GetBytes(json);

            using (var request = new UnityWebRequest(url, "PATCH"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyBytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {config.accessToken}");
                request.SetRequestHeader("Accept", "application/vnd.github+json");
                request.SetRequestHeader("User-Agent", "Melpomene-Unity");

                try
                {
                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        Debug.Log($"[Melpomene] Issue #{issueNumber} closed");
                        return true;
                    }
                    else
                    {
                        Debug.LogError($"[Melpomene] Failed to close issue: {request.error}");
                        return false;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Melpomene] Exception closing issue: {e.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// パース結果を保持する構造体
        /// </summary>
        private struct ParsedIssueData
        {
            public GitHubIssueResponse issue;
            public string[] labels;
        }

        /// <summary>
        /// JSON配列をパースする（JsonUtilityは配列に対応していないため）
        /// </summary>
        private List<ParsedIssueData> ParseIssueArray(string jsonArray)
        {
            var issues = new List<ParsedIssueData>();

            if (string.IsNullOrEmpty(jsonArray) || jsonArray.Trim() == "[]")
            {
                Debug.Log("[Melpomene] Empty issue array received");
                return issues;
            }

            // 手動でパースを試みる
            // 各Issueオブジェクトを個別に抽出
            int depth = 0;
            int start = -1;
            bool inString = false;
            bool escape = false;

            for (int i = 0; i < jsonArray.Length; i++)
            {
                char c = jsonArray[i];

                // エスケープシーケンスの処理
                if (escape)
                {
                    escape = false;
                    continue;
                }

                if (c == '\\' && inString)
                {
                    escape = true;
                    continue;
                }

                // 文字列内外の判定
                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }

                // 文字列内では括弧をカウントしない
                if (inString)
                {
                    continue;
                }

                if (c == '{')
                {
                    if (depth == 0)
                    {
                        start = i;
                    }
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0 && start >= 0)
                    {
                        var issueJson = jsonArray.Substring(start, i - start + 1);
                        try
                        {
                            var issue = JsonUtility.FromJson<GitHubIssueResponse>(issueJson);
                            if (issue != null && issue.number > 0)
                            {
                                // ラベルを手動で抽出
                                var labels = ExtractLabelsFromJson(issueJson);
                                issues.Add(new ParsedIssueData { issue = issue, labels = labels });
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"[Melpomene] Failed to parse issue JSON: {e.Message}");
                        }
                        start = -1;
                    }
                }
            }

            return issues;
        }

        #region JSON Data Classes

        [Serializable]
        private class GitHubIssueRequest
        {
            public string title;
            public string body;
            public string[] labels;
        }

        [Serializable]
        private class GitHubIssueResponse
        {
            public int number;
            public string title;
            public string body;
            public string html_url;
            public string state;
            public string created_at;
            // NOTE: labelsはJsonUtilityでネストした配列をパースできないため、手動でパースする
        }

        #endregion

        /// <summary>
        /// JSONからラベル名の配列を手動で抽出する
        /// NOTE: JsonUtilityはネストした配列をパースできないため
        /// </summary>
        private string[] ExtractLabelsFromJson(string issueJson)
        {
            var labels = new List<string>();

            // "labels":[...] を探す
            int labelsStart = issueJson.IndexOf("\"labels\":");
            if (labelsStart < 0)
                return labels.ToArray();

            int arrayStart = issueJson.IndexOf('[', labelsStart);
            if (arrayStart < 0)
                return labels.ToArray();

            int arrayEnd = issueJson.IndexOf(']', arrayStart);
            if (arrayEnd < 0)
                return labels.ToArray();

            string labelsSection = issueJson.Substring(arrayStart, arrayEnd - arrayStart + 1);

            // "name":"xxx" を探す
            int searchPos = 0;
            while (true)
            {
                int namePos = labelsSection.IndexOf("\"name\":", searchPos);
                if (namePos < 0)
                    break;

                int valueStart = labelsSection.IndexOf('"', namePos + 7);
                if (valueStart < 0)
                    break;

                int valueEnd = labelsSection.IndexOf('"', valueStart + 1);
                if (valueEnd < 0)
                    break;

                string labelName = labelsSection.Substring(valueStart + 1, valueEnd - valueStart - 1);
                labels.Add(labelName);

                searchPos = valueEnd + 1;
            }

            return labels.ToArray();
        }
    }
}
#endif
