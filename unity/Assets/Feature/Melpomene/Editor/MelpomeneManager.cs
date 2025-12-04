#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Cysharp.Threading.Tasks;

namespace Melpomene
{
    /// <summary>
    /// Melpomeneツールのメイン管理クラス
    /// NOTE: シングルトン。ツール全体の管理を行う
    /// </summary>
    [InitializeOnLoad]
    public class MelpomeneManager
    {
        private static MelpomeneManager instance;
        private MelpomeneConfig config;
        private MelpomeneCache cache;
        private MelpomeneGitHubClient gitHubClient;
        private bool isInitialized;

        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static MelpomeneManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MelpomeneManager();
                }
                return instance;
            }
        }

        /// <summary>
        /// 設定
        /// </summary>
        public MelpomeneConfig Config => config;

        /// <summary>
        /// キャッシュ
        /// </summary>
        public MelpomeneCache Cache => cache;

        /// <summary>
        /// 初期化済みかどうか
        /// </summary>
        public bool IsInitialized => isInitialized;

        /// <summary>
        /// 設定が有効かどうか
        /// </summary>
        public bool IsConfigValid => config != null && config.IsValid;

        /// <summary>
        /// 静的コンストラクタ（エディタ起動時に実行）
        /// </summary>
        static MelpomeneManager()
        {
            // エディタ起動時に遅延初期化をスケジュール
            EditorApplication.delayCall += () =>
            {
                Instance.Initialize();
            };
        }

        private MelpomeneManager()
        {
        }

        /// <summary>
        /// 初期化
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
                return;

            config = MelpomeneConfig.GetOrCreateConfig();
            cache = new MelpomeneCache(config.cacheDurationMinutes);
            gitHubClient = new MelpomeneGitHubClient(config);

            // SceneView上のイベントをフック
            SceneView.duringSceneGui += OnSceneGUI;

            // Hierarchyウィンドウ上のイベントをフック
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemGUI;

            isInitialized = true;
            Debug.Log("[Melpomene] Initialized");

            // 初期キャッシュ取得
            if (config.IsValid)
            {
                RefreshCacheAsync().Forget();
            }
        }

        /// <summary>
        /// チケットを作成する
        /// </summary>
        public async UniTask<MelpomeneTicket> CreateTicketAsync(MelpomeneTicket ticket)
        {
            if (!IsConfigValid)
            {
                Debug.LogError("[Melpomene] Config is not valid. Please configure in Tools/Melpomene/Settings");
                return null;
            }

            // タイムスタンプを設定
            ticket.timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // GitHubにIssueを作成
            var createdTicket = await gitHubClient.CreateIssueAsync(ticket);

            if (createdTicket != null)
            {
                // キャッシュに追加
                cache.AddTicket(createdTicket);
            }

            return createdTicket;
        }

        /// <summary>
        /// キャッシュを更新する
        /// </summary>
        public async UniTask RefreshCacheAsync()
        {
            if (!IsConfigValid)
            {
                Debug.LogWarning("[Melpomene] Config is not valid. Skipping cache refresh.");
                return;
            }

            var tickets = await gitHubClient.GetIssuesAsync();
            cache.Update(tickets);
        }

        /// <summary>
        /// キャッシュが期限切れの場合のみ更新
        /// </summary>
        public async UniTask RefreshCacheIfExpiredAsync()
        {
            if (cache.IsExpired)
            {
                await RefreshCacheAsync();
            }
        }

        /// <summary>
        /// 現在のシーンのチケットを取得
        /// </summary>
        public List<MelpomeneTicket> GetTicketsForCurrentScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            return cache.GetByScene(scene.name);
        }

        /// <summary>
        /// 指定シーンのチケットを取得
        /// </summary>
        public List<MelpomeneTicket> GetTicketsForScene(string sceneName)
        {
            return cache.GetByScene(sceneName);
        }

        /// <summary>
        /// Issueをクローズする
        /// </summary>
        public async UniTask<bool> CloseTicketAsync(int issueNumber)
        {
            var result = await gitHubClient.CloseIssueAsync(issueNumber);
            if (result)
            {
                cache.RemoveTicket(issueNumber);
            }
            return result;
        }

        /// <summary>
        /// SceneView上のGUIイベント処理
        /// NOTE: Alt+クリックでチケット入力UIを表示
        /// </summary>
        private void OnSceneGUI(SceneView sceneView)
        {
            // Alt+Clickショートカットが無効の場合は処理しない
            if (config == null || !config.enableAltClickShortcut)
                return;

            var e = Event.current;

            // Alt + Ctrl + 左クリック
            if (e.type == EventType.MouseDown && e.button == 0 && e.alt && e.control)
            {
                // クリック位置を取得
                Vector2 screenPosition = e.mousePosition;

                // Raycastで対象オブジェクトを特定
                Ray ray = HandleUtility.GUIPointToWorldRay(screenPosition);
                GameObject targetObject = null;
                Vector3 worldPosition = Vector3.zero;

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    targetObject = hit.collider.gameObject;
                    worldPosition = hit.point;
                }
                else
                {
                    // Raycastがヒットしない場合はシーンカメラからの距離で位置を計算
                    worldPosition = ray.GetPoint(10f);
                }

                // チケット入力ウィンドウを表示
                MelpomeneInputWindow.ShowWindow(screenPosition, worldPosition, targetObject);

                e.Use();
            }
        }

        /// <summary>
        /// Hierarchyウィンドウ上のGUIイベント処理
        /// NOTE: Alt+クリックでチケット入力UIを表示
        /// </summary>
        private void OnHierarchyWindowItemGUI(int instanceID, Rect selectionRect)
        {
            // Alt+Clickショートカットが無効の場合は処理しない
            if (config == null || !config.enableAltClickShortcut)
                return;

            var e = Event.current;

            // Alt + Ctrl + 左クリック かつ アイテム上でのクリック
            if (e.type == EventType.MouseDown && e.button == 0 && e.alt && e.control && selectionRect.Contains(e.mousePosition))
            {
                // InstanceIDからGameObjectを取得
                var targetObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

                if (targetObject != null)
                {
                    // スクリーン座標はマウス位置を使用
                    Vector2 screenPosition = e.mousePosition;

                    // ワールド座標はオブジェクトの位置を使用
                    Vector3 worldPosition = targetObject.transform.position;

                    // チケット入力ウィンドウを表示
                    MelpomeneInputWindow.ShowWindow(screenPosition, worldPosition, targetObject);

                    e.Use();
                }
            }
        }

        /// <summary>
        /// 新規チケットを作成するためのデータを準備
        /// </summary>
        public MelpomeneTicket PrepareNewTicket(Vector2 screenPosition, Vector3 worldPosition, GameObject targetObject)
        {
            var scene = EditorSceneManager.GetActiveScene();

            var ticket = new MelpomeneTicket
            {
                userName = config.defaultUserName,
                sceneName = scene.name,
                screenPosition = screenPosition,
                worldPosition = worldPosition,
                targetObjectPath = targetObject != null ? GetHierarchyPath(targetObject) : "",
                priority = config.defaultPriority,
                category = config.defaultCategory
            };

            return ticket;
        }

        /// <summary>
        /// GameObjectのHierarchyパスを取得
        /// </summary>
        private string GetHierarchyPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
    }
}
#endif
