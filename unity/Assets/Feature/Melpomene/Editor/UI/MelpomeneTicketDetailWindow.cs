#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Melpomene
{
    /// <summary>
    /// Melpomeneチケット詳細ウィンドウ
    /// NOTE: チケットの詳細情報を表示
    /// </summary>
    public class MelpomeneTicketDetailWindow : EditorWindow
    {
        private MelpomeneTicket ticket;
        private Vector2 scrollPosition;
        private GameObject newTargetObject;
        private bool isEditingTarget;

        /// <summary>
        /// ウィンドウを表示
        /// </summary>
        public static void ShowWindow(MelpomeneTicket ticket)
        {
            var window = GetWindow<MelpomeneTicketDetailWindow>();
            window.titleContent = new GUIContent($"Ticket #{ticket.issueNumber}");
            window.ticket = ticket;
            window.minSize = new Vector2(400, 400);
            window.Show();
        }

        private void OnGUI()
        {
            if (ticket == null)
            {
                EditorGUILayout.HelpBox("No ticket selected.", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // ヘッダー
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"#{ticket.issueNumber}", EditorStyles.boldLabel, GUILayout.Width(60));

            // 状態バッジ
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = ticket.state == "open" ? Color.green : Color.red;
            GUILayout.Label(ticket.state.ToUpper(), "box", GUILayout.Width(60));
            GUI.backgroundColor = oldColor;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // タイトル
            EditorGUILayout.LabelField("Title", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(ticket.title, EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space();

            // 説明
            EditorGUILayout.LabelField("Description", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(ticket.description ?? "(No description)", EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space();

            // メタデータ
            EditorGUILayout.LabelField("Metadata", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("User", ticket.userName);
            EditorGUILayout.LabelField("Scene", ticket.sceneName);

            // Target Object - 編集可能
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Object", string.IsNullOrEmpty(ticket.targetObjectPath) ? "(none)" : ticket.targetObjectPath);
            if (GUILayout.Button(isEditingTarget ? "Cancel" : "Edit", GUILayout.Width(60)))
            {
                isEditingTarget = !isEditingTarget;
                if (isEditingTarget)
                {
                    // 現在のパスからオブジェクトを探す
                    newTargetObject = !string.IsNullOrEmpty(ticket.targetObjectPath)
                        ? GameObject.Find(ticket.targetObjectPath)
                        : null;
                }
            }
            EditorGUILayout.EndHorizontal();

            // Target Object編集UI
            if (isEditingTarget)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical("box");

                newTargetObject = (GameObject)EditorGUILayout.ObjectField(
                    "New Target",
                    newTargetObject,
                    typeof(GameObject),
                    true
                );

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Apply"))
                {
                    ApplyNewTargetObject();
                    isEditingTarget = false;
                }
                if (GUILayout.Button("Clear Target"))
                {
                    ClearTargetObject();
                    isEditingTarget = false;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.LabelField("Priority", ticket.priority.ToString());
            EditorGUILayout.LabelField("Category", ticket.category.ToString());
            EditorGUILayout.LabelField("Created", ticket.timestamp);
            EditorGUILayout.Vector2Field("Screen Position", ticket.screenPosition);
            EditorGUILayout.Vector3Field("World Position", ticket.worldPosition);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(20);

            // アクションボタン
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Open in Browser", GUILayout.Height(25)))
            {
                if (!string.IsNullOrEmpty(ticket.issueUrl))
                {
                    Application.OpenURL(ticket.issueUrl);
                }
            }

            if (GUILayout.Button("Copy URL", GUILayout.Height(25)))
            {
                GUIUtility.systemCopyBuffer = ticket.issueUrl;
                Debug.Log($"[Melpomene] URL copied: {ticket.issueUrl}");
            }

            if (GUILayout.Button("Go to Position", GUILayout.Height(25)))
            {
                // シーンビューをチケットの位置にフォーカス
                SceneView.lastActiveSceneView?.LookAt(ticket.worldPosition);
            }

            EditorGUILayout.EndHorizontal();

            // 対象オブジェクトへのフォーカス
            if (!string.IsNullOrEmpty(ticket.targetObjectPath))
            {
                if (GUILayout.Button("Select Target Object", GUILayout.Height(25)))
                {
                    var obj = GameObject.Find(ticket.targetObjectPath);
                    if (obj != null)
                    {
                        Selection.activeGameObject = obj;
                        SceneView.lastActiveSceneView?.FrameSelected();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Melpomene", $"Object not found: {ticket.targetObjectPath}", "OK");
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 新しいTarget Objectを適用
        /// </summary>
        private void ApplyNewTargetObject()
        {
            if (newTargetObject != null)
            {
                ticket.targetObjectPath = GetHierarchyPath(newTargetObject);
                ticket.worldPosition = newTargetObject.transform.position;
                Debug.Log($"[Melpomene] Target object updated: {ticket.targetObjectPath}");
            }
            Repaint();
        }

        /// <summary>
        /// Target Objectをクリア
        /// </summary>
        private void ClearTargetObject()
        {
            ticket.targetObjectPath = "";
            newTargetObject = null;
            Debug.Log("[Melpomene] Target object cleared");
            Repaint();
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
