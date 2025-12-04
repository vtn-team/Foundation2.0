#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Melpomene
{
    /// <summary>
    /// Melpomeneチケットのキャッシュ管理
    /// NOTE: GitHubから取得したチケットをローカルにキャッシュ
    /// </summary>
    public class MelpomeneCache
    {
        private readonly int cacheDurationMinutes;
        private DateTime lastFetchTime = DateTime.MinValue;
        private List<MelpomeneTicket> tickets = new List<MelpomeneTicket>();

        public MelpomeneCache(int cacheDurationMinutes)
        {
            this.cacheDurationMinutes = cacheDurationMinutes;
        }

        /// <summary>
        /// 最終取得時刻
        /// </summary>
        public DateTime LastFetchTime => lastFetchTime;

        /// <summary>
        /// キャッシュされたチケット一覧
        /// </summary>
        public IReadOnlyList<MelpomeneTicket> Tickets => tickets;

        /// <summary>
        /// キャッシュが有効期限切れかどうか
        /// </summary>
        public bool IsExpired
        {
            get
            {
                if (lastFetchTime == DateTime.MinValue)
                    return true;

                var elapsed = DateTime.Now - lastFetchTime;
                return elapsed.TotalMinutes >= cacheDurationMinutes;
            }
        }

        /// <summary>
        /// キャッシュを更新する
        /// </summary>
        public void Update(List<MelpomeneTicket> newTickets)
        {
            tickets = newTickets ?? new List<MelpomeneTicket>();
            lastFetchTime = DateTime.Now;
            Debug.Log($"[Melpomene] Cache updated with {tickets.Count} tickets");
        }

        /// <summary>
        /// チケットを追加する（新規作成時）
        /// </summary>
        public void AddTicket(MelpomeneTicket ticket)
        {
            if (ticket != null)
            {
                tickets.Add(ticket);
            }
        }

        /// <summary>
        /// チケットを削除する
        /// </summary>
        public void RemoveTicket(int issueNumber)
        {
            tickets.RemoveAll(t => t.issueNumber == issueNumber);
        }

        /// <summary>
        /// シーン名でフィルタしてチケットを取得
        /// </summary>
        public List<MelpomeneTicket> GetByScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return new List<MelpomeneTicket>();

            return tickets
                .Where(t => t.sceneName == sceneName)
                .ToList();
        }

        /// <summary>
        /// オブジェクトパスでチケットを取得
        /// </summary>
        public MelpomeneTicket GetByObjectPath(string sceneName, string objectPath)
        {
            return tickets.FirstOrDefault(t =>
                t.sceneName == sceneName &&
                t.targetObjectPath == objectPath
            );
        }

        /// <summary>
        /// ワールド座標でチケットを検索（近傍検索）
        /// </summary>
        public List<MelpomeneTicket> GetNearPosition(string sceneName, Vector3 worldPosition, float radius = 1f)
        {
            return tickets
                .Where(t =>
                    t.sceneName == sceneName &&
                    Vector3.Distance(t.worldPosition, worldPosition) <= radius
                )
                .ToList();
        }

        /// <summary>
        /// キャッシュをクリアする
        /// </summary>
        public void Clear()
        {
            tickets.Clear();
            lastFetchTime = DateTime.MinValue;
        }

        /// <summary>
        /// 統計情報を取得
        /// </summary>
        public string GetStats()
        {
            var byScene = tickets.GroupBy(t => t.sceneName).ToDictionary(g => g.Key, g => g.Count());
            var byPriority = tickets.GroupBy(t => t.priority).ToDictionary(g => g.Key, g => g.Count());
            var byCategory = tickets.GroupBy(t => t.category).ToDictionary(g => g.Key, g => g.Count());

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Total Tickets: {tickets.Count}");
            sb.AppendLine($"Last Fetch: {lastFetchTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Cache Expired: {IsExpired}");
            sb.AppendLine();
            sb.AppendLine("By Scene:");
            foreach (var kvp in byScene)
            {
                sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
            }
            sb.AppendLine();
            sb.AppendLine("By Priority:");
            foreach (var kvp in byPriority)
            {
                sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
            }
            sb.AppendLine();
            sb.AppendLine("By Category:");
            foreach (var kvp in byCategory)
            {
                sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
            }

            return sb.ToString();
        }
    }
}
#endif
