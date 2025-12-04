#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Cysharp.Threading.Tasks;

namespace Melpomene
{
    /// <summary>
    /// Melpomeneチケット入力ウィンドウ
    /// NOTE: Alt+クリックで表示されるチケット入力UI
    /// </summary>
    public class MelpomeneInputWindow : EditorWindow
    {
        private MelpomeneTicket ticket;
        private Vector2 scrollPosition;
        private bool isSending;
        private GameObject targetObject;

        private static MelpomeneInputWindow currentWindow;

        /// <summary>
        /// ウィンドウを表示
        /// </summary>
        public static void ShowWindow(Vector2 screenPosition, Vector3 worldPosition, GameObject targetObject)
        {
            // 既存のウィンドウがあれば閉じる
            if (currentWindow != null)
            {
                currentWindow.Close();
            }

            var window = CreateInstance<MelpomeneInputWindow>();
            window.titleContent = new GUIContent("Melpomene - New Ticket");
            window.minSize = new Vector2(400, 500);
            window.maxSize = new Vector2(600, 800);

            // チケットデータを準備
            window.ticket = MelpomeneManager.Instance.PrepareNewTicket(screenPosition, worldPosition, targetObject);
            window.targetObject = targetObject;

            // ウィンドウをマウス位置の近くに表示
            var mousePos = GUIUtility.GUIToScreenPoint(Event.current?.mousePosition ?? Vector2.zero);
            window.position = new Rect(mousePos.x + 20, mousePos.y - 100, 450, 550);

            window.ShowUtility();
            currentWindow = window;
        }

        private void OnGUI()
        {
            if (ticket == null)
            {
                EditorGUILayout.HelpBox("Ticket data is not initialized.", MessageType.Error);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUI.BeginDisabledGroup(isSending);

            // ヘッダー
            EditorGUILayout.LabelField("New Ticket", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 必須項目
            EditorGUILayout.LabelField("Required Fields", EditorStyles.boldLabel);

            ticket.userName = EditorGUILayout.TextField("User Name", ticket.userName);
            ticket.title = EditorGUILayout.TextField("Title", ticket.title);

            EditorGUILayout.LabelField("Description");
            ticket.description = EditorGUILayout.TextArea(ticket.description, GUILayout.Height(100));

            EditorGUILayout.Space();

            // 自動取得項目
            EditorGUILayout.LabelField("Target Info", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Scene", ticket.sceneName);
            EditorGUI.EndDisabledGroup();

            // Target Object - 編集可能
            EditorGUI.BeginChangeCheck();
            targetObject = (GameObject)EditorGUILayout.ObjectField(
                "Target Object",
                targetObject,
                typeof(GameObject),
                true
            );
            if (EditorGUI.EndChangeCheck())
            {
                UpdateTargetObject();
            }

            // Target Object Path（読み取り専用で表示）
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Object Path", string.IsNullOrEmpty(ticket.targetObjectPath) ? "(none)" : ticket.targetObjectPath);
            EditorGUILayout.Vector2Field("Screen Position", ticket.screenPosition);
            EditorGUILayout.Vector3Field("World Position", ticket.worldPosition);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            // オプション項目
            EditorGUILayout.LabelField("Optional Settings", EditorStyles.boldLabel);

            ticket.priority = (MelpomenePriority)EditorGUILayout.EnumPopup("Priority", ticket.priority);
            ticket.category = (MelpomeneCategory)EditorGUILayout.EnumPopup("Category", ticket.category);
            ticket.labels = EditorGUILayout.TextField("Additional Labels", ticket.labels);

            EditorGUILayout.Space(20);

            EditorGUI.EndDisabledGroup();

            // バリデーション
            bool isValid = ValidateTicket();

            if (!isValid)
            {
                EditorGUILayout.HelpBox("Please fill in all required fields (User Name, Title, Description)", MessageType.Warning);
            }

            if (!MelpomeneManager.Instance.IsConfigValid)
            {
                EditorGUILayout.HelpBox("GitHub configuration is not set. Please configure in Tools/Melpomene/Settings", MessageType.Error);

                if (GUILayout.Button("Open Settings"))
                {
                    MelpomeneConfig.OpenSettings();
                }
            }

            EditorGUILayout.Space();

            // ボタン
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Cancel", GUILayout.Height(30)))
            {
                Close();
            }

            EditorGUI.BeginDisabledGroup(!isValid || !MelpomeneManager.Instance.IsConfigValid || isSending);

            if (GUILayout.Button(isSending ? "Sending..." : "Create Issue", GUILayout.Height(30)))
            {
                CreateIssueAsync().Forget();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        private bool ValidateTicket()
        {
            return !string.IsNullOrWhiteSpace(ticket.userName) &&
                   !string.IsNullOrWhiteSpace(ticket.title) &&
                   !string.IsNullOrWhiteSpace(ticket.description);
        }

        private async UniTaskVoid CreateIssueAsync()
        {
            isSending = true;
            Repaint();

            try
            {
                var createdTicket = await MelpomeneManager.Instance.CreateTicketAsync(ticket);

                if (createdTicket != null)
                {
                    EditorUtility.DisplayDialog(
                        "Melpomene",
                        $"Issue #{createdTicket.issueNumber} created successfully!\n\n{createdTicket.issueUrl}",
                        "OK"
                    );

                    // URLをクリップボードにコピー
                    GUIUtility.systemCopyBuffer = createdTicket.issueUrl;

                    Close();
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "Melpomene",
                        "Failed to create issue. Please check the console for errors.",
                        "OK"
                    );
                }
            }
            finally
            {
                isSending = false;
                Repaint();
            }
        }

        private void OnDestroy()
        {
            if (currentWindow == this)
            {
                currentWindow = null;
            }
        }

        /// <summary>
        /// Target Objectが変更されたときにticketの情報を更新
        /// </summary>
        private void UpdateTargetObject()
        {
            if (targetObject != null)
            {
                ticket.targetObjectPath = GetHierarchyPath(targetObject);
                ticket.worldPosition = targetObject.transform.position;
            }
            else
            {
                ticket.targetObjectPath = "";
            }
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
