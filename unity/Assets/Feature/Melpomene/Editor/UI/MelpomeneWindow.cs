#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Cysharp.Threading.Tasks;

namespace Melpomene
{
    /// <summary>
    /// Melpomeneメインウィンドウ
    /// NOTE: チケット一覧の表示と管理
    /// </summary>
    public class MelpomeneWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private string searchQuery = "";
        private MelpomeneCategory? filterCategory = null;
        private MelpomenePriority? filterPriority = null;
        private bool showCurrentSceneOnly = true;
        private bool isRefreshing;

        private List<MelpomeneTicket> filteredTickets = new List<MelpomeneTicket>();

        [MenuItem("Tools/Melpomene/Ticket List")]
        public static void ShowWindow()
        {
            var window = GetWindow<MelpomeneWindow>();
            window.titleContent = new GUIContent("Melpomene");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnEnable()
        {
            // 初期化確認
            if (!MelpomeneManager.Instance.IsInitialized)
            {
                MelpomeneManager.Instance.Initialize();
            }

            RefreshFilteredTickets();
        }

        private void OnGUI()
        {
            DrawToolbar();

            EditorGUILayout.Space();

            if (!MelpomeneManager.Instance.IsConfigValid)
            {
                EditorGUILayout.HelpBox("GitHub configuration is not set. Please configure in Tools/Melpomene/Settings", MessageType.Warning);

                if (GUILayout.Button("Open Settings"))
                {
                    MelpomeneConfig.OpenSettings();
                }

                EditorGUILayout.Space();
            }

            DrawFilters();

            EditorGUILayout.Space();

            DrawTicketList();

            DrawFooter();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshCacheAsync().Forget();
            }

            if (GUILayout.Button("New Ticket", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                // シーンビューの中心位置でチケット作成
                var sceneView = SceneView.lastActiveSceneView;
                Vector3 worldPos = sceneView != null ? sceneView.pivot : Vector3.zero;
                MelpomeneInputWindow.ShowWindow(Vector2.zero, worldPos, null);
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Settings", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                MelpomeneConfig.OpenSettings();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawFilters()
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel, GUILayout.Width(50));
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                searchQuery = "";
                filterCategory = null;
                filterPriority = null;
                RefreshFilteredTickets();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            // 検索
            EditorGUILayout.LabelField("Search", GUILayout.Width(50));
            var newQuery = EditorGUILayout.TextField(searchQuery);
            if (newQuery != searchQuery)
            {
                searchQuery = newQuery;
                RefreshFilteredTickets();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            // カテゴリフィルタ
            EditorGUILayout.LabelField("Category", GUILayout.Width(60));
            var categoryOptions = new string[] { "All", "Bug", "Feature", "Improvement", "Question" };
            int categoryIndex = filterCategory.HasValue ? (int)filterCategory.Value + 1 : 0;
            int newCategoryIndex = EditorGUILayout.Popup(categoryIndex, categoryOptions, GUILayout.Width(100));
            if (newCategoryIndex != categoryIndex)
            {
                filterCategory = newCategoryIndex == 0 ? null : (MelpomeneCategory?)(newCategoryIndex - 1);
                RefreshFilteredTickets();
            }

            // 優先度フィルタ
            EditorGUILayout.LabelField("Priority", GUILayout.Width(50));
            var priorityOptions = new string[] { "All", "Low", "Medium", "High", "Critical" };
            int priorityIndex = filterPriority.HasValue ? (int)filterPriority.Value + 1 : 0;
            int newPriorityIndex = EditorGUILayout.Popup(priorityIndex, priorityOptions, GUILayout.Width(80));
            if (newPriorityIndex != priorityIndex)
            {
                filterPriority = newPriorityIndex == 0 ? null : (MelpomenePriority?)(newPriorityIndex - 1);
                RefreshFilteredTickets();
            }

            // 現在のシーンのみ
            var newShowCurrentScene = EditorGUILayout.ToggleLeft("Current Scene Only", showCurrentSceneOnly, GUILayout.Width(130));
            if (newShowCurrentScene != showCurrentSceneOnly)
            {
                showCurrentSceneOnly = newShowCurrentScene;
                RefreshFilteredTickets();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawTicketList()
        {
            EditorGUILayout.LabelField($"Tickets ({filteredTickets.Count})", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (filteredTickets.Count == 0)
            {
                EditorGUILayout.HelpBox("No tickets found.", MessageType.Info);
            }
            else
            {
                foreach (var ticket in filteredTickets)
                {
                    DrawTicketItem(ticket);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawTicketItem(MelpomeneTicket ticket)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();

            // チケット番号
            EditorGUILayout.LabelField($"#{ticket.issueNumber}", EditorStyles.boldLabel, GUILayout.Width(50));

            // カテゴリバッジ
            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = GetCategoryColor(ticket.category);
            GUILayout.Label(ticket.category.ToString(), "box", GUILayout.Width(80));

            // 優先度バッジ
            GUI.backgroundColor = GetPriorityColor(ticket.priority);
            GUILayout.Label(ticket.priority.ToString(), "box", GUILayout.Width(60));
            GUI.backgroundColor = oldBg;

            GUILayout.FlexibleSpace();

            // アクションボタン
            if (GUILayout.Button("View", GUILayout.Width(45)))
            {
                MelpomeneTicketDetailWindow.ShowWindow(ticket);
            }

            if (GUILayout.Button("Go", GUILayout.Width(30)))
            {
                SceneView.lastActiveSceneView?.LookAt(ticket.worldPosition);
            }

            EditorGUILayout.EndHorizontal();

            // タイトル
            EditorGUILayout.LabelField(ticket.title, EditorStyles.wordWrappedLabel);

            // メタ情報
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Scene: {ticket.sceneName}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"By: {ticket.userName}", EditorStyles.miniLabel, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal();

            var cache = MelpomeneManager.Instance.Cache;
            string cacheInfo = cache != null
                ? $"Last updated: {cache.LastFetchTime:HH:mm:ss} | {(cache.IsExpired ? "Expired" : "Valid")}"
                : "Cache not initialized";

            EditorGUILayout.LabelField(cacheInfo, EditorStyles.miniLabel);

            if (isRefreshing)
            {
                GUILayout.Label("Refreshing...", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void RefreshFilteredTickets()
        {
            var cache = MelpomeneManager.Instance.Cache;
            if (cache == null)
            {
                filteredTickets.Clear();
                return;
            }

            IEnumerable<MelpomeneTicket> tickets = cache.Tickets;

            // 現在のシーンフィルタ
            if (showCurrentSceneOnly)
            {
                var currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;
                tickets = System.Linq.Enumerable.Where(tickets, t => t.sceneName == currentScene);
            }

            // カテゴリフィルタ
            if (filterCategory.HasValue)
            {
                var category = filterCategory.Value;
                tickets = System.Linq.Enumerable.Where(tickets, t => t.category == category);
            }

            // 優先度フィルタ
            if (filterPriority.HasValue)
            {
                var priority = filterPriority.Value;
                tickets = System.Linq.Enumerable.Where(tickets, t => t.priority == priority);
            }

            // 検索フィルタ
            if (!string.IsNullOrEmpty(searchQuery))
            {
                var query = searchQuery.ToLower();
                tickets = System.Linq.Enumerable.Where(tickets, t =>
                    (t.title?.ToLower().Contains(query) ?? false) ||
                    (t.description?.ToLower().Contains(query) ?? false) ||
                    (t.userName?.ToLower().Contains(query) ?? false)
                );
            }

            filteredTickets = new List<MelpomeneTicket>(tickets);
            Repaint();
        }

        private async UniTaskVoid RefreshCacheAsync()
        {
            isRefreshing = true;
            Repaint();

            try
            {
                await MelpomeneManager.Instance.RefreshCacheAsync();
                RefreshFilteredTickets();
            }
            finally
            {
                isRefreshing = false;
                Repaint();
            }
        }

        private Color GetCategoryColor(MelpomeneCategory category)
        {
            return category switch
            {
                MelpomeneCategory.Bug => new Color(1f, 0.3f, 0.3f),
                MelpomeneCategory.Feature => new Color(0.3f, 0.7f, 1f),
                MelpomeneCategory.Improvement => new Color(0.3f, 1f, 0.5f),
                MelpomeneCategory.Question => new Color(1f, 0.8f, 0.3f),
                _ => Color.gray
            };
        }

        private Color GetPriorityColor(MelpomenePriority priority)
        {
            return priority switch
            {
                MelpomenePriority.Critical => new Color(1f, 0f, 0f),
                MelpomenePriority.High => new Color(1f, 0.5f, 0f),
                MelpomenePriority.Medium => new Color(1f, 1f, 0f),
                MelpomenePriority.Low => new Color(0.7f, 0.7f, 0.7f),
                _ => Color.gray
            };
        }
    }
}
#endif
