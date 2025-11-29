using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BoxSelectionクラスのテストコンポーネント
/// </summary>
public class BoxSelectionTest : MonoBehaviour
{
    [SerializeField]
    [Tooltip("テスト用のBoxSelectionSheet")]
    private BoxSelectionSheet sheet;

    [SerializeField]
    [Tooltip("DrawManyで何回試行するか")]
    private int drawNum = 100;

    [SerializeField]
    [Tooltip("ランダムシード")]
    private int randomSeed = 12345;

    private BoxSelection _boxSelect;

    private void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// 初期化
    /// </summary>
    public void Initialize()
    {
        if (sheet != null)
        {
            _boxSelect = new BoxSelection(sheet, randomSeed);
        }
    }

    /// <summary>
    /// 1回テスト。何が出たかをログで表示
    /// </summary>
    [ContextMenu("Draw One")]
    public void DrawOne()
    {
        if (_boxSelect == null)
        {
            Debug.LogWarning("[BoxSelectionTest] BoxSelection is not initialized");
            return;
        }

        var result = _boxSelect.Pop();
        if (result != null)
        {
            Debug.Log($"[BoxSelectionTest] Drew: ID={result.Id}, Key={result.PrefabKey}, Prop={result.Prop}");
        }
        else
        {
            Debug.Log("[BoxSelectionTest] Box is empty!");
        }
    }

    /// <summary>
    /// N回テスト。何が何回出たかをサマリでログで表示
    /// </summary>
    [ContextMenu("Draw Many")]
    public void DrawMany()
    {
        if (_boxSelect == null)
        {
            Debug.LogWarning("[BoxSelectionTest] BoxSelection is not initialized");
            return;
        }

        // リセット
        _boxSelect.ResetWithNewSeed(randomSeed);

        var resultCount = new Dictionary<string, int>();
        int emptyCount = 0;

        for (int i = 0; i < drawNum; i++)
        {
            var result = _boxSelect.Pop();
            if (result != null)
            {
                string key = result.PrefabKey ?? "Unknown";
                if (resultCount.ContainsKey(key))
                {
                    resultCount[key]++;
                }
                else
                {
                    resultCount[key] = 1;
                }
            }
            else
            {
                emptyCount++;
            }
        }

        // サマリ表示
        Debug.Log($"[BoxSelectionTest] === Draw Many Results ({drawNum} draws) ===");
        foreach (var pair in resultCount)
        {
            float percentage = (float)pair.Value / drawNum * 100;
            Debug.Log($"  {pair.Key}: {pair.Value} times ({percentage:F1}%)");
        }
        if (emptyCount > 0)
        {
            Debug.Log($"  Empty: {emptyCount} times");
        }
        Debug.Log($"[BoxSelectionTest] Remaining in box: {_boxSelect.RemainingCount}");
    }

    /// <summary>
    /// ボックスをリセット
    /// </summary>
    [ContextMenu("Reset Box")]
    public void ResetBox()
    {
        if (_boxSelect != null)
        {
            _boxSelect.Reset();
            Debug.Log("[BoxSelectionTest] Box reset");
        }
    }
}
