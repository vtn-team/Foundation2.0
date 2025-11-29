using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using R3;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
/// <summary>
/// デバッグメニューのUI表示を担当するMonoBehaviour
/// </summary>
public class DebugPromptView : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField]
    [Tooltip("UI表示用のUIDocumentコンポーネント")]
    private UIDocument uiDocument;

    [Header("UXML Templates")]
    [SerializeField]
    [Tooltip("メインレイアウトのUXMLテンプレート")]
    private VisualTreeAsset mainTemplate;

    [SerializeField]
    [Tooltip("ボタンウィジェットのUXMLテンプレート")]
    private VisualTreeAsset buttonTemplate;

    [SerializeField]
    [Tooltip("トグルウィジェットのUXMLテンプレート")]
    private VisualTreeAsset toggleTemplate;

    [Header("Settings")]
    [SerializeField]
    [Tooltip("UIDocumentの描画順")]
    private int sortingOrder = 30000;

    private VisualElement _root;
    private VisualElement _menuPanel;
    private VisualElement _contentContainer;
    private Label _titleText;
    private Subject<IDebugCommand> _onCommandExecuted = new Subject<IDebugCommand>();

    /// <summary>
    /// 表示中かどうか
    /// </summary>
    public bool IsVisible { get; private set; }

    /// <summary>
    /// コマンド実行時のイベント
    /// </summary>
    public Observable<IDebugCommand> OnCommandExecuted => _onCommandExecuted;

    private void Awake()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument != null)
        {
            uiDocument.sortingOrder = sortingOrder;
        }

        InitializeUI();
    }

    private void OnDestroy()
    {
        _onCommandExecuted.Dispose();
    }

    /// <summary>
    /// UIを初期化
    /// </summary>
    private void InitializeUI()
    {
        if (uiDocument == null) return;

        // テンプレートがある場合は使用、なければ動的生成
        if (mainTemplate != null)
        {
            _root = mainTemplate.Instantiate();
            uiDocument.rootVisualElement.Add(_root);
            _menuPanel = _root.Q<VisualElement>("menu-panel");
            _contentContainer = _root.Q<VisualElement>("content-container");
            _titleText = _root.Q<Label>("title-text");
        }
        else
        {
            CreateFallbackUI();
        }

        Hide();
    }

    /// <summary>
    /// フォールバックUI作成
    /// </summary>
    private void CreateFallbackUI()
    {
        _root = new VisualElement();
        _root.name = "overlay-root";
        _root.style.position = Position.Absolute;
        _root.style.left = 0;
        _root.style.top = 0;
        _root.style.right = 0;
        _root.style.bottom = 0;
        _root.style.backgroundColor = new Color(0, 0, 0, 0.5f);

        _menuPanel = new VisualElement();
        _menuPanel.name = "menu-panel";
        _menuPanel.style.position = Position.Absolute;
        _menuPanel.style.left = 20;
        _menuPanel.style.top = 20;
        _menuPanel.style.width = 300;
        _menuPanel.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        _menuPanel.style.paddingTop = 10;
        _menuPanel.style.paddingBottom = 10;
        _menuPanel.style.paddingLeft = 10;
        _menuPanel.style.paddingRight = 10;
        _root.Add(_menuPanel);

        _titleText = new Label("Debug Menu");
        _titleText.name = "title-text";
        _titleText.style.fontSize = 18;
        _titleText.style.color = Color.white;
        _titleText.style.marginBottom = 10;
        _menuPanel.Add(_titleText);

        var scrollView = new ScrollView();
        scrollView.name = "scroll-view";
        _menuPanel.Add(scrollView);

        _contentContainer = new VisualElement();
        _contentContainer.name = "content-container";
        scrollView.Add(_contentContainer);

        uiDocument.rootVisualElement.Add(_root);
    }

    /// <summary>
    /// デバッグメニューを表示
    /// </summary>
    public void Show()
    {
        if (_root != null)
        {
            _root.style.display = DisplayStyle.Flex;
        }
        IsVisible = true;
    }

    /// <summary>
    /// デバッグメニューを非表示
    /// </summary>
    public void Hide()
    {
        if (_root != null)
        {
            _root.style.display = DisplayStyle.None;
        }
        IsVisible = false;
    }

    /// <summary>
    /// コマンドリストからUIを構築
    /// </summary>
    /// <param name="commands">コマンドリスト</param>
    public void BuildMenu(IReadOnlyList<IDebugCommand> commands)
    {
        ClearWidgets();

        if (commands == null) return;

        for (int i = 0; i < commands.Count; i++)
        {
            var command = commands[i];
            var element = CreateCommandWidget(command, i);
            _contentContainer.Add(element);
        }
    }

    /// <summary>
    /// コマンドウィジェットを作成
    /// </summary>
    private VisualElement CreateCommandWidget(IDebugCommand command, int index)
    {
        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Row;
        container.style.marginBottom = 5;

        // ショートカットラベル
        var shortcutLabel = new Label($"[{index + 1}]");
        shortcutLabel.style.width = 30;
        shortcutLabel.style.color = Color.yellow;
        container.Add(shortcutLabel);

        // コマンドUI
        var commandElement = command.CreateMenuElement();
        commandElement.style.flexGrow = 1;
        container.Add(commandElement);

        return container;
    }

    /// <summary>
    /// メニューのタイトルを設定
    /// </summary>
    /// <param name="title">タイトル</param>
    public void SetTitle(string title)
    {
        if (_titleText != null)
        {
            _titleText.text = title;
        }
    }

    /// <summary>
    /// 全てのウィジェットをクリア
    /// </summary>
    public void ClearWidgets()
    {
        if (_contentContainer != null)
        {
            _contentContainer.Clear();
        }
    }

    /// <summary>
    /// ラベルウィジェットを作成
    /// </summary>
    /// <param name="text">テキスト</param>
    public void CreateLabel(string text)
    {
        if (_contentContainer == null) return;

        var label = new Label(text);
        label.style.marginTop = 10;
        label.style.marginBottom = 5;
        label.style.color = Color.gray;
        _contentContainer.Add(label);
    }
}
#endif
