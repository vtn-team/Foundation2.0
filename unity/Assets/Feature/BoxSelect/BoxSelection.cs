using System;
using System.Collections.Generic;

/// <summary>
/// 重みづけ抽選を行うボックスガチャクラス
/// </summary>
public class BoxSelection
{
    private List<BoxSelectObjectData> _boxList = new List<BoxSelectObjectData>();
    private Random _random;
    private BoxSelectionSheet _boxSheet;
    private int _randomSeed;

    /// <summary>
    /// ボックスシート
    /// </summary>
    public BoxSelectionSheet BoxSheet
    {
        get => _boxSheet;
        set => _boxSheet = value;
    }

    /// <summary>
    /// ランダムシード
    /// </summary>
    public int RandomSeed
    {
        get => _randomSeed;
        set => _randomSeed = value;
    }

    /// <summary>
    /// ボックス内の残り個数
    /// </summary>
    public int RemainingCount
    {
        get
        {
            int count = 0;
            foreach (var item in _boxList)
            {
                if (item.InStock) count++;
            }
            return count;
        }
    }

    /// <summary>
    /// ボックスが空かどうか
    /// </summary>
    public bool IsEmpty => RemainingCount == 0;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="sheet">ボックスシート</param>
    /// <param name="seed">ランダムシード</param>
    public BoxSelection(BoxSelectionSheet sheet, int seed)
    {
        _boxSheet = sheet;
        _randomSeed = seed;
        Initialize();
    }

    /// <summary>
    /// 初期化
    /// </summary>
    public void Initialize()
    {
        _boxList.Clear();
        _random = new Random(_randomSeed);

        if (_boxSheet == null || _boxSheet.objectDataList == null) return;

        int id = 0;
        foreach (var data in _boxSheet.objectDataList)
        {
            // numの数だけボックスに展開
            for (int i = 0; i < data.num; i++)
            {
                var boxData = new BoxSelectObjectData
                {
                    Id = id++,
                    Prop = data.prop,
                    PrefabKey = data.prefabKey,
                    InStock = true
                };
                _boxList.Add(boxData);
            }
        }
    }

    /// <summary>
    /// ボックスから1つ選択して取り出す
    /// </summary>
    /// <returns>選択されたオブジェクトデータ（ボックスが空の場合はnull）</returns>
    public BoxSelectObjectData Pop()
    {
        // 在庫があるアイテムの合計重みを計算
        float totalWeight = 0f;
        foreach (var item in _boxList)
        {
            if (item.InStock)
            {
                totalWeight += item.Prop;
            }
        }

        if (totalWeight <= 0f)
        {
            return null; // ボックスが空
        }

        // 重みづけ抽選
        float randomValue = (float)_random.NextDouble() * totalWeight;
        float currentWeight = 0f;

        foreach (var item in _boxList)
        {
            if (!item.InStock) continue;

            currentWeight += item.Prop;
            if (randomValue <= currentWeight)
            {
                item.InStock = false;
                return item;
            }
        }

        // フォールバック（通常は到達しない）
        foreach (var item in _boxList)
        {
            if (item.InStock)
            {
                item.InStock = false;
                return item;
            }
        }

        return null;
    }

    /// <summary>
    /// オブジェクトをボックスリストに戻す
    /// </summary>
    /// <param name="data">戻すオブジェクトデータ</param>
    /// <returns>戻せた場合はtrue</returns>
    public bool ReturnObject(BoxSelectObjectData data)
    {
        if (data == null) return false;

        foreach (var item in _boxList)
        {
            if (item.Id == data.Id)
            {
                item.InStock = true;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// オブジェクトをIDで戻す
    /// </summary>
    /// <param name="id">オブジェクトID</param>
    /// <returns>戻せた場合はtrue</returns>
    public bool ReturnObject(int id)
    {
        foreach (var item in _boxList)
        {
            if (item.Id == id)
            {
                item.InStock = true;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// ボックスをリセット（全てのアイテムを在庫に戻す）
    /// </summary>
    public void Reset()
    {
        foreach (var item in _boxList)
        {
            item.InStock = true;
        }
    }

    /// <summary>
    /// シードを変更して再初期化
    /// </summary>
    /// <param name="newSeed">新しいシード値</param>
    public void ResetWithNewSeed(int newSeed)
    {
        _randomSeed = newSeed;
        Initialize();
    }
}
