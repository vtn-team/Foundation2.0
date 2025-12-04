#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using Cysharp.Threading.Tasks;

namespace Melpomene
{
    /// <summary>
    /// SceneView上にMelpomeneチケットを表示するUI Toolkitベースのオーバーレイ
    /// NOTE: 各チケットをボタンとして表示し、クリックでIssueを開く
    /// </summary>
    [InitializeOnLoad]
    public static class MelpomeneSceneViewOverlay
    {
        private static readonly Dictionary<SceneView, VisualElement> sceneViewContainers = new();
        private static readonly Dictionary<int, MelpomeneTicketElement> ticketElements = new();
        private static StyleSheet styleSheet;

        static MelpomeneSceneViewOverlay()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            // 定期的にチケット位置を更新
            foreach (var kvp in sceneViewContainers)
            {
                if (kvp.Key != null)
                {
                    UpdateTicketPositions(kvp.Key, kvp.Value);
                }
            }
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!MelpomeneManager.Instance.IsInitialized)
                return;

            // SceneViewごとにコンテナを管理
            if (!sceneViewContainers.TryGetValue(sceneView, out var container))
            {
                container = CreateContainer(sceneView);
                sceneViewContainers[sceneView] = container;
            }

            // チケットを更新
            UpdateTickets(sceneView, container);
        }

        /// <summary>
        /// コンテナを作成
        /// </summary>
        private static VisualElement CreateContainer(SceneView sceneView)
        {
            var container = new VisualElement
            {
                name = "melpomene-ticket-container",
                pickingMode = PickingMode.Ignore
            };

            container.style.position = Position.Absolute;
            container.style.left = 0;
            container.style.top = 0;
            container.style.right = 0;
            container.style.bottom = 0;

            // スタイルシートを読み込み
            if (styleSheet == null)
            {
                styleSheet = LoadStyleSheet();
            }
            if (styleSheet != null)
            {
                container.styleSheets.Add(styleSheet);
            }

            sceneView.rootVisualElement.Add(container);
            return container;
        }

        /// <summary>
        /// スタイルシートを読み込み
        /// </summary>
        private static StyleSheet LoadStyleSheet()
        {
            var guids = AssetDatabase.FindAssets("MelpomeneSceneView t:StyleSheet");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            }
            return null;
        }

        /// <summary>
        /// チケットを更新
        /// </summary>
        private static void UpdateTickets(SceneView sceneView, VisualElement container)
        {
            var tickets = MelpomeneManager.Instance.GetTicketsForCurrentScene();

            // 既存の要素をマーク
            var existingNumbers = new HashSet<int>(ticketElements.Keys);

            foreach (var ticket in tickets)
            {
                if (!ticketElements.TryGetValue(ticket.issueNumber, out var element))
                {
                    // 新規チケット要素を作成
                    element = new MelpomeneTicketElement(ticket);
                    container.Add(element);
                    ticketElements[ticket.issueNumber] = element;
                }
                else
                {
                    // 既存要素を更新
                    element.UpdateTicket(ticket);
                    existingNumbers.Remove(ticket.issueNumber);
                }
            }

            // 不要な要素を削除
            foreach (var number in existingNumbers)
            {
                if (ticketElements.TryGetValue(number, out var element))
                {
                    element.RemoveFromHierarchy();
                    ticketElements.Remove(number);
                }
            }
        }

        /// <summary>
        /// チケット位置を更新
        /// </summary>
        private static void UpdateTicketPositions(SceneView sceneView, VisualElement container)
        {
            if (sceneView.camera == null)
                return;

            foreach (var kvp in ticketElements)
            {
                var element = kvp.Value;
                var worldPos = element.GetWorldPosition();

                // ワールド座標をスクリーン座標に変換
                var viewportPos = sceneView.camera.WorldToViewportPoint(worldPos);

                // カメラの後ろにある場合は非表示
                if (viewportPos.z < 0)
                {
                    element.style.display = DisplayStyle.None;
                    continue;
                }

                // ビューポート外の場合は非表示
                if (viewportPos.x < 0 || viewportPos.x > 1 || viewportPos.y < 0 || viewportPos.y > 1)
                {
                    element.style.display = DisplayStyle.None;
                    continue;
                }

                element.style.display = DisplayStyle.Flex;

                // スクリーン座標に変換（Y軸反転）
                var screenPos = new Vector2(
                    viewportPos.x * sceneView.position.width,
                    (1 - viewportPos.y) * sceneView.position.height
                );

                // 要素の中心が位置に来るように調整
                element.style.left = screenPos.x - element.resolvedStyle.width / 2;
                element.style.top = screenPos.y - element.resolvedStyle.height / 2;
            }
        }

        /// <summary>
        /// キャッシュをクリア（シーン変更時などに呼び出し）
        /// </summary>
        public static void ClearCache()
        {
            foreach (var element in ticketElements.Values)
            {
                element.RemoveFromHierarchy();
            }
            ticketElements.Clear();
        }
    }

    /// <summary>
    /// 個別のチケット表示要素
    /// </summary>
    public class MelpomeneTicketElement : VisualElement
    {
        private MelpomeneTicket ticket;
        private readonly Button button;
        private readonly VisualElement categoryIndicator;
        private readonly Label numberLabel;
        private readonly Label titleLabel;

        private static readonly Color BugColor = new Color(1f, 0.3f, 0.3f, 0.9f);
        private static readonly Color FeatureColor = new Color(0.3f, 0.7f, 1f, 0.9f);
        private static readonly Color ImprovementColor = new Color(0.3f, 1f, 0.5f, 0.9f);
        private static readonly Color QuestionColor = new Color(1f, 0.8f, 0.3f, 0.9f);

        private static readonly Color CriticalColor = new Color(0.9f, 0.1f, 0.1f, 0.95f);
        private static readonly Color HighColor = new Color(1f, 0.5f, 0f, 0.95f);
        private static readonly Color MediumColor = new Color(0.3f, 0.7f, 0.3f, 0.95f);
        private static readonly Color LowColor = new Color(0.4f, 0.4f, 0.5f, 0.95f);

        public MelpomeneTicketElement(MelpomeneTicket ticket)
        {
            this.ticket = ticket;

            // 基本設定
            AddToClassList("melpomene-ticket");
            style.position = Position.Absolute;
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;

            // ボタン作成
            button = new Button(() => OnClick())
            {
                name = "ticket-button"
            };
            button.AddToClassList("melpomene-ticket-button");
            Add(button);

            // カテゴリインジケーター（右上の小さな丸）
            categoryIndicator = new VisualElement
            {
                name = "category-indicator"
            };
            categoryIndicator.AddToClassList("melpomene-category-indicator");
            button.Add(categoryIndicator);

            // チケット番号ラベル
            numberLabel = new Label
            {
                name = "ticket-number"
            };
            numberLabel.AddToClassList("melpomene-ticket-number");
            button.Add(numberLabel);

            // タイトルラベル（ボタンの横）
            titleLabel = new Label
            {
                name = "ticket-title"
            };
            titleLabel.AddToClassList("melpomene-ticket-title");
            Add(titleLabel);

            // ツールチップ
            button.tooltip = "";

            // 右クリックメニュー登録
            button.AddManipulator(new ContextualMenuManipulator(OnRightClick));

            // 初期更新
            UpdateVisuals();
        }

        public void UpdateTicket(MelpomeneTicket newTicket)
        {
            ticket = newTicket;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            // 番号
            numberLabel.text = $"#{ticket.issueNumber}";

            // タイトル
            titleLabel.text = ticket.title ?? "";

            // 優先度色（ボタンの背景色）
            var priorityColor = GetPriorityColor(ticket.priority);
            button.style.backgroundColor = priorityColor;

            // カテゴリ色（右上インジケーター）
            var categoryColor = GetCategoryColor(ticket.category);
            categoryIndicator.style.backgroundColor = categoryColor;

            // ツールチップ
            button.tooltip = $"#{ticket.issueNumber}: {ticket.title}\n{ticket.category} - {ticket.priority}";
        }

        /// <summary>
        /// ワールド座標を取得
        /// NOTE: Hierarchyパスからオブジェクトを復元できれば現在位置を使用
        /// </summary>
        public Vector3 GetWorldPosition()
        {
            if (!string.IsNullOrEmpty(ticket.targetObjectPath))
            {
                var obj = GameObject.Find(ticket.targetObjectPath);
                if (obj != null)
                {
                    return obj.transform.position;
                }
            }
            return ticket.worldPosition;
        }

        private void OnClick()
        {
            // チケット詳細ウィンドウを開く（TicketListと同じ挙動）
            MelpomeneTicketDetailWindow.ShowWindow(ticket);
        }

        private void OnRightClick(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction($"#{ticket.issueNumber}: {ticket.title}", null, DropdownMenuAction.Status.Disabled);
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Open in Browser", _ =>
            {
                if (!string.IsNullOrEmpty(ticket.issueUrl))
                {
                    Application.OpenURL(ticket.issueUrl);
                }
            });
            evt.menu.AppendAction("Copy URL", _ =>
            {
                GUIUtility.systemCopyBuffer = ticket.issueUrl;
            });
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("View Details", _ =>
            {
                MelpomeneTicketDetailWindow.ShowWindow(ticket);
            });

            if (ticket.state == "open")
            {
                evt.menu.AppendAction("Close Issue", _ =>
                {
                    if (EditorUtility.DisplayDialog("Close Issue", $"Close issue #{ticket.issueNumber}?", "Yes", "No"))
                    {
                        MelpomeneManager.Instance.CloseTicketAsync(ticket.issueNumber).Forget();
                    }
                });
            }
        }

        private Color GetCategoryColor(MelpomeneCategory category)
        {
            return category switch
            {
                MelpomeneCategory.Bug => BugColor,
                MelpomeneCategory.Feature => FeatureColor,
                MelpomeneCategory.Improvement => ImprovementColor,
                MelpomeneCategory.Question => QuestionColor,
                _ => Color.gray
            };
        }

        private Color GetPriorityColor(MelpomenePriority priority)
        {
            return priority switch
            {
                MelpomenePriority.Critical => CriticalColor,
                MelpomenePriority.High => HighColor,
                MelpomenePriority.Medium => MediumColor,
                MelpomenePriority.Low => LowColor,
                _ => Color.gray
            };
        }
    }
}
#endif
