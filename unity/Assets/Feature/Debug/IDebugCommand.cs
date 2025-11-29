using UnityEngine.UIElements;

/// <summary>
/// デバッグコマンドの基本インターフェース
/// </summary>
public interface IDebugCommand
{
    /// <summary>
    /// コマンド名
    /// </summary>
    string Name { get; }

    /// <summary>
    /// コマンドの説明
    /// </summary>
    string Description { get; }

    /// <summary>
    /// コマンドを実行する
    /// </summary>
    void Execute();

    /// <summary>
    /// コマンドの状態をリセットする
    /// </summary>
    void Reset();

    /// <summary>
    /// メニューのUI要素を作成する
    /// </summary>
    /// <returns>UI要素</returns>
    VisualElement CreateMenuElement();

    /// <summary>
    /// 任意のボタン押下時に一度だけ実行されるイベント
    /// </summary>
    void OnButtonPressedOnce();

    /// <summary>
    /// 任意のボタン押下時に常に実行されるイベント
    /// </summary>
    void OnButtonPressed();

    /// <summary>
    /// メニューの機能呼び出し時に実行されるイベント
    /// </summary>
    void OnMenuInvoked();
}

/// <summary>
/// デバッグコマンドの基底クラス
/// </summary>
public abstract class DebugCommandBase : IDebugCommand
{
    /// <summary>
    /// コマンド名
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// コマンドの説明
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// コマンドを実行する
    /// </summary>
    public abstract void Execute();

    /// <summary>
    /// コマンドの状態をリセットする
    /// </summary>
    public virtual void Reset() { }

    /// <summary>
    /// メニューのUI要素を作成する
    /// </summary>
    public virtual VisualElement CreateMenuElement()
    {
        var button = new Button(Execute)
        {
            text = Name
        };
        button.tooltip = Description;
        return button;
    }

    /// <summary>
    /// 任意のボタン押下時に一度だけ実行されるイベント
    /// </summary>
    public virtual void OnButtonPressedOnce() { }

    /// <summary>
    /// 任意のボタン押下時に常に実行されるイベント
    /// </summary>
    public virtual void OnButtonPressed() { }

    /// <summary>
    /// メニューの機能呼び出し時に実行されるイベント
    /// </summary>
    public virtual void OnMenuInvoked() { }
}

/// <summary>
/// トグル型デバッグコマンドの基底クラス
/// </summary>
public abstract class DebugToggleCommandBase : IDebugCommand
{
    private bool _isEnabled;

    /// <summary>
    /// コマンド名
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// コマンドの説明
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// 有効かどうか
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            OnToggleChanged(value);
        }
    }

    /// <summary>
    /// コマンドを実行する（トグル切り替え）
    /// </summary>
    public void Execute()
    {
        IsEnabled = !IsEnabled;
    }

    /// <summary>
    /// コマンドの状態をリセットする
    /// </summary>
    public virtual void Reset()
    {
        IsEnabled = false;
    }

    /// <summary>
    /// メニューのUI要素を作成する
    /// </summary>
    public virtual VisualElement CreateMenuElement()
    {
        var toggle = new Toggle(Name)
        {
            value = _isEnabled
        };
        toggle.tooltip = Description;
        toggle.RegisterValueChangedCallback(evt => IsEnabled = evt.newValue);
        return toggle;
    }

    /// <summary>
    /// トグル変更時のコールバック
    /// </summary>
    protected abstract void OnToggleChanged(bool isEnabled);

    /// <summary>
    /// 任意のボタン押下時に一度だけ実行されるイベント
    /// </summary>
    public virtual void OnButtonPressedOnce() { }

    /// <summary>
    /// 任意のボタン押下時に常に実行されるイベント
    /// </summary>
    public virtual void OnButtonPressed() { }

    /// <summary>
    /// メニューの機能呼び出し時に実行されるイベント
    /// </summary>
    public virtual void OnMenuInvoked() { }
}
