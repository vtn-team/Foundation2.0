using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
/// <summary>
/// ゲーム中のデバッグコマンドを管理するコントローラークラス
/// </summary>
public class DebugPrompt
{
    private DebugPromptData _data;
    private DebugPromptView _view;
    private List<IDebugCommand> _commands = new List<IDebugCommand>();
    private bool _isMenuOpen;

    /// <summary>
    /// デバッグデータ
    /// </summary>
    public DebugPromptData Data => _data;

    /// <summary>
    /// ビュー
    /// </summary>
    public DebugPromptView View => _view;

    /// <summary>
    /// 登録されているコマンドリスト
    /// </summary>
    public IReadOnlyList<IDebugCommand> Commands => _commands;

    /// <summary>
    /// メニューが開いているかどうか
    /// </summary>
    public bool IsMenuOpen => _isMenuOpen;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="data">デバッグデータ</param>
    /// <param name="view">ビュー</param>
    public DebugPrompt(DebugPromptData data, DebugPromptView view)
    {
        _data = data ?? new DebugPromptData();
        _view = view;
    }

    /// <summary>
    /// デバッグコマンドを登録
    /// </summary>
    /// <param name="command">登録するコマンド</param>
    public void RegisterCommand(IDebugCommand command)
    {
        if (command != null && !_commands.Contains(command))
        {
            _commands.Add(command);
        }
    }

    /// <summary>
    /// デバッグコマンドを登録解除
    /// </summary>
    /// <param name="command">解除するコマンド</param>
    public void UnregisterCommand(IDebugCommand command)
    {
        _commands.Remove(command);
    }

    /// <summary>
    /// 更新処理
    /// </summary>
    public void Update()
    {
        bool shouldShowMenu = CheckMenuTrigger();

        if (shouldShowMenu && !_isMenuOpen)
        {
            OpenMenu();
        }
        else if (!shouldShowMenu && _isMenuOpen)
        {
            CloseMenu();
        }

        if (_isMenuOpen)
        {
            UpdateMenuInput();
        }
    }

    /// <summary>
    /// メニュートリガーをチェック
    /// </summary>
    private bool CheckMenuTrigger()
    {
        // キーボード: Left Ctrl
        if (Keyboard.current != null && Keyboard.current.leftCtrlKey.isPressed)
        {
            return true;
        }

        // ゲームパッド: L2トリガー
        if (Gamepad.current != null && Gamepad.current.leftTrigger.isPressed)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// メニューを開く
    /// </summary>
    private void OpenMenu()
    {
        _isMenuOpen = true;

        if (_view != null)
        {
            _view.BuildMenu(_commands);
            _view.Show();
        }

        // ゲーム停止
        if (_data.StopGameFrame)
        {
            Time.timeScale = 0f;
        }
    }

    /// <summary>
    /// メニューを閉じる
    /// </summary>
    private void CloseMenu()
    {
        _isMenuOpen = false;

        if (_view != null)
        {
            _view.Hide();
        }

        // ゲーム復元
        if (_data.StopGameFrame)
        {
            Time.timeScale = 1f;
        }
    }

    /// <summary>
    /// メニュー入力更新
    /// </summary>
    private void UpdateMenuInput()
    {
        if (Keyboard.current == null) return;

        // 1-9キーでコマンド実行
        for (int i = 0; i < 9 && i < _commands.Count; i++)
        {
            Key key = (Key)((int)Key.Digit1 + i);
            if (Keyboard.current[key].wasPressedThisFrame)
            {
                ExecuteCommand(i);
            }
        }
    }

    /// <summary>
    /// インデックスでコマンドを実行
    /// </summary>
    /// <param name="index">コマンドインデックス</param>
    public void ExecuteCommand(int index)
    {
        if (index >= 0 && index < _commands.Count)
        {
            _commands[index].Execute();
        }
    }

    /// <summary>
    /// 名前でコマンドを実行
    /// </summary>
    /// <param name="name">コマンド名</param>
    public void ExecuteCommand(string name)
    {
        foreach (var command in _commands)
        {
            if (command.Name == name)
            {
                command.Execute();
                return;
            }
        }
    }
}
#endif
