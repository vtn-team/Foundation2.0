using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// デバッグプロンプトの設定データ
/// セーブデータとして永続化される
/// </summary>
[Serializable]
public class DebugPromptData
{
    [SerializeField]
    [Tooltip("デバッグメニュー表示中にゲームを一時停止するか")]
    private bool stopGameFrame = false;

    [SerializeField]
    [Tooltip("デバッグメニュー表示中にゲーム入力を無効化するか")]
    private bool useAnotherKeymap = true;

    [SerializeField]
    private SerializableDictionary<string, bool> toggleStates = new SerializableDictionary<string, bool>();

    [SerializeField]
    private SerializableDictionary<string, float> valueStates = new SerializableDictionary<string, float>();

    /// <summary>
    /// デバッグメニュー表示中にゲームを一時停止するか
    /// </summary>
    public bool StopGameFrame
    {
        get => stopGameFrame;
        set => stopGameFrame = value;
    }

    /// <summary>
    /// デバッグメニュー表示中にゲーム入力を無効化するか
    /// </summary>
    public bool UseAnotherKeymap
    {
        get => useAnotherKeymap;
        set => useAnotherKeymap = value;
    }

    /// <summary>
    /// トグルコマンドの状態を取得
    /// </summary>
    /// <param name="key">コマンド名</param>
    /// <returns>トグル状態</returns>
    public bool GetToggleState(string key)
    {
        if (toggleStates.TryGetValue(key, out bool value))
        {
            return value;
        }
        return false;
    }

    /// <summary>
    /// トグルコマンドの状態を設定
    /// </summary>
    /// <param name="key">コマンド名</param>
    /// <param name="value">トグル状態</param>
    public void SetToggleState(string key, bool value)
    {
        toggleStates[key] = value;
    }

    /// <summary>
    /// 値コマンドの状態を取得
    /// </summary>
    /// <param name="key">コマンド名</param>
    /// <param name="defaultValue">デフォルト値</param>
    /// <returns>値</returns>
    public float GetValueState(string key, float defaultValue = 0f)
    {
        if (valueStates.TryGetValue(key, out float value))
        {
            return value;
        }
        return defaultValue;
    }

    /// <summary>
    /// 値コマンドの状態を設定
    /// </summary>
    /// <param name="key">コマンド名</param>
    /// <param name="value">値</param>
    public void SetValueState(string key, float value)
    {
        valueStates[key] = value;
    }

    /// <summary>
    /// 全ての設定をデフォルト値にリセット
    /// </summary>
    public void ResetToDefault()
    {
        stopGameFrame = false;
        useAnotherKeymap = true;
        toggleStates.Clear();
        valueStates.Clear();
    }
}

/// <summary>
/// シリアライズ可能な辞書クラス
/// </summary>
[Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField]
    private List<TKey> keys = new List<TKey>();

    [SerializeField]
    private List<TValue> values = new List<TValue>();

    public void OnBeforeSerialize()
    {
        keys.Clear();
        values.Clear();

        foreach (var pair in this)
        {
            keys.Add(pair.Key);
            values.Add(pair.Value);
        }
    }

    public void OnAfterDeserialize()
    {
        Clear();

        int count = Math.Min(keys.Count, values.Count);
        for (int i = 0; i < count; i++)
        {
            this[keys[i]] = values[i];
        }
    }
}
