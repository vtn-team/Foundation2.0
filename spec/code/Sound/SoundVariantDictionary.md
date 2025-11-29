# SoundVariantDictionary

## 概要
SoundDictionaryを継承したバリエーション対応サウンドディクショナリ。
再生キー1つに対して複数のAudioClipバリエーションを設定でき、インデックスを指定することで該当のAudioClipを取得できる。
同じサウンドキーで複数の音声パターンを用意し、状況に応じて使い分けることが可能。

## 責務
- 再生キーに対する複数AudioClipのバリエーション管理
- インデックス指定によるAudioClip取得
- バリエーション数の取得
- SoundDictionaryとの互換性維持（SoundManagerから利用可能）

## データ構造

### SoundClipVariant（AudioClipバリエーション）
| パラメータ | 型 | 説明 |
|-----------|-----|------|
| Clip | AudioClip | 音声ファイル |
| Label | string | バリエーションのラベル（任意、エディタ表示用） |

### SoundVariantDictionaryItem（バリエーションアイテム）
| パラメータ | 型 | 説明 |
|-----------|-----|------|
| Key | string | 再生キー（一意の識別子） |
| Variants | List&lt;SoundClipVariant&gt; | バリエーションリスト |
| GroupId | int | グループID |
| Priority | int | 優先度 |

## クラス定義

```csharp
namespace BaseSystem.Sound.InGame
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// AudioClipバリエーション
    /// </summary>
    [Serializable]
    public class SoundClipVariant
    {
        [SerializeField]
        [Tooltip("音声ファイル")]
        private AudioClip clip;

        [SerializeField]
        [Tooltip("バリエーションのラベル（任意）")]
        private string label;

        /// <summary>
        /// AudioClip
        /// </summary>
        public AudioClip Clip => clip;

        /// <summary>
        /// ラベル
        /// </summary>
        public string Label => label;

        /// <summary>
        /// 有効かどうか
        /// </summary>
        public bool IsValid => clip != null;

        /// <summary>
        /// AudioClipの長さ（秒）
        /// </summary>
        public float Length => clip != null ? clip.length : 0f;
    }

    /// <summary>
    /// バリエーション対応サウンドアイテム
    /// </summary>
    [Serializable]
    public class SoundVariantDictionaryItem
    {
        [Header("Identification")]
        [SerializeField]
        [Tooltip("再生キー（一意の識別子）")]
        private string key;

        [Header("Variants")]
        [SerializeField]
        [Tooltip("AudioClipバリエーションリスト")]
        private List<SoundClipVariant> variants = new List<SoundClipVariant>();

        [Header("Control")]
        [SerializeField]
        [Tooltip("グループID")]
        private int groupId;

        [SerializeField]
        [Range(0, 100)]
        [Tooltip("優先度")]
        private int priority = 50;

        /// <summary>
        /// 再生キー
        /// </summary>
        public string Key => key;

        /// <summary>
        /// バリエーションリスト
        /// </summary>
        public IReadOnlyList<SoundClipVariant> Variants => variants;

        /// <summary>
        /// グループID
        /// </summary>
        public int GroupId => groupId;

        /// <summary>
        /// 優先度
        /// </summary>
        public int Priority => priority;

        /// <summary>
        /// サウンドタイプ（親のSoundVariantDictionaryから参照）
        /// </summary>
        public SoundType SoundType { get; internal set; }

        /// <summary>
        /// カテゴリ（親のSoundVariantDictionaryから参照）
        /// </summary>
        public string Category { get; internal set; }

        /// <summary>
        /// バリエーション数
        /// </summary>
        public int VariantCount => variants.Count;

        /// <summary>
        /// 有効なバリエーション数
        /// </summary>
        public int ValidVariantCount
        {
            get
            {
                int count = 0;
                foreach (var v in variants)
                {
                    if (v.IsValid) count++;
                }
                return count;
            }
        }

        /// <summary>
        /// 有効かどうか
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(key) && variants.Count > 0 && HasValidVariant();

        /// <summary>
        /// 有効なバリエーションが存在するか
        /// </summary>
        private bool HasValidVariant()
        {
            foreach (var v in variants)
            {
                if (v.IsValid) return true;
            }
            return false;
        }

        /// <summary>
        /// 指定インデックスのAudioClipを取得
        /// </summary>
        /// <param name="index">バリエーションインデックス</param>
        /// <returns>AudioClip（範囲外またはnullの場合はnull）</returns>
        public AudioClip GetClipByIndex(int index)
        {
            if (index < 0 || index >= variants.Count)
            {
                return null;
            }
            return variants[index].Clip;
        }

        /// <summary>
        /// 最初の有効なAudioClipを取得
        /// </summary>
        /// <returns>最初の有効なAudioClip</returns>
        public AudioClip GetFirstClip()
        {
            foreach (var v in variants)
            {
                if (v.IsValid) return v.Clip;
            }
            return null;
        }

        /// <summary>
        /// 指定インデックスのバリエーション情報を取得
        /// </summary>
        /// <param name="index">バリエーションインデックス</param>
        /// <returns>SoundClipVariant（範囲外の場合はnull）</returns>
        public SoundClipVariant GetVariant(int index)
        {
            if (index < 0 || index >= variants.Count)
            {
                return null;
            }
            return variants[index];
        }

        /// <summary>
        /// ラベルでバリエーションを検索
        /// </summary>
        /// <param name="label">検索するラベル</param>
        /// <returns>該当するAudioClip（見つからない場合はnull）</returns>
        public AudioClip GetClipByLabel(string label)
        {
            foreach (var v in variants)
            {
                if (v.Label == label && v.IsValid)
                {
                    return v.Clip;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// バリエーション対応サウンドディクショナリ
    /// </summary>
    [CreateAssetMenu(fileName = "SoundVariantDictionary", menuName = "Sound/SoundVariantDictionary")]
    public class SoundVariantDictionary : SoundDictionary
    {
        [Header("Variant Items")]
        [SerializeField]
        [Tooltip("バリエーションアイテムリスト")]
        private List<SoundVariantDictionaryItem> variantItems = new List<SoundVariantDictionaryItem>();

        // バリエーションアイテムのキャッシュ
        private Dictionary<string, SoundVariantDictionaryItem> variantItemMap;

        /// <summary>
        /// バリエーションアイテムリスト
        /// </summary>
        public IReadOnlyList<SoundVariantDictionaryItem> VariantItems => variantItems;

        /// <summary>
        /// バリエーションアイテム数
        /// </summary>
        public int VariantItemCount => variantItems.Count;

        /// <summary>
        /// 初期化（オーバーライド）
        /// </summary>
        public override void Initialize()
        {
            if (isInitialized) return;

            // 基底クラスの初期化
            base.Initialize();

            // バリエーションアイテムマップの初期化
            variantItemMap = new Dictionary<string, SoundVariantDictionaryItem>();

            foreach (var item in variantItems)
            {
                if (!item.IsValid) continue;

                // 親情報を設定
                item.SoundType = soundType;
                item.Category = category;

                if (!variantItemMap.ContainsKey(item.Key))
                {
                    variantItemMap[item.Key] = item;
                }
                else
                {
                    Debug.LogWarning($"[SoundVariantDictionary] 重複キー検出: {item.Key} in {name}");
                }
            }

            Debug.Log($"[SoundVariantDictionary] {variantItemMap.Count}個のバリエーションアイテムを読み込み: {name}");
        }

        /// <summary>
        /// バリエーションアイテムを取得
        /// </summary>
        /// <param name="key">再生キー</param>
        /// <returns>SoundVariantDictionaryItem（存在しない場合はnull）</returns>
        public SoundVariantDictionaryItem GetVariantItem(string key)
        {
            if (!isInitialized) Initialize();

            variantItemMap.TryGetValue(key, out var item);
            return item;
        }

        /// <summary>
        /// バリエーションアイテムを取得（Try形式）
        /// </summary>
        /// <param name="key">再生キー</param>
        /// <param name="item">出力アイテム</param>
        /// <returns>存在する場合はtrue</returns>
        public bool TryGetVariantItem(string key, out SoundVariantDictionaryItem item)
        {
            if (!isInitialized) Initialize();

            return variantItemMap.TryGetValue(key, out item);
        }

        /// <summary>
        /// 指定インデックスのAudioClipを取得
        /// </summary>
        /// <param name="key">再生キー</param>
        /// <param name="index">バリエーションインデックス</param>
        /// <returns>AudioClip（存在しない場合はnull）</returns>
        public AudioClip GetClipByIndex(string key, int index)
        {
            if (TryGetVariantItem(key, out var item))
            {
                return item.GetClipByIndex(index);
            }
            return null;
        }

        /// <summary>
        /// 最初の有効なAudioClipを取得
        /// </summary>
        /// <param name="key">再生キー</param>
        /// <returns>最初の有効なAudioClip</returns>
        public AudioClip GetFirstClip(string key)
        {
            if (TryGetVariantItem(key, out var item))
            {
                return item.GetFirstClip();
            }
            return null;
        }

        /// <summary>
        /// 指定キーのバリエーション数を取得
        /// </summary>
        /// <param name="key">再生キー</param>
        /// <returns>バリエーション数（キーが存在しない場合は0）</returns>
        public int GetVariantCount(string key)
        {
            if (TryGetVariantItem(key, out var item))
            {
                return item.VariantCount;
            }
            return 0;
        }

        /// <summary>
        /// 指定キーの有効なバリエーション数を取得
        /// </summary>
        /// <param name="key">再生キー</param>
        /// <returns>有効なバリエーション数（キーが存在しない場合は0）</returns>
        public int GetValidVariantCount(string key)
        {
            if (TryGetVariantItem(key, out var item))
            {
                return item.ValidVariantCount;
            }
            return 0;
        }

        /// <summary>
        /// バリエーションキーが存在するか確認
        /// </summary>
        /// <param name="key">再生キー</param>
        /// <returns>存在する場合はtrue</returns>
        public bool ContainsVariantKey(string key)
        {
            if (!isInitialized) Initialize();

            return variantItemMap.ContainsKey(key);
        }

        /// <summary>
        /// 全バリエーションキーを取得
        /// </summary>
        /// <returns>キーの列挙</returns>
        public IEnumerable<string> GetAllVariantKeys()
        {
            if (!isInitialized) Initialize();

            return variantItemMap.Keys;
        }
    }
}
```

## 使用例

### ScriptableObjectの作成
1. Projectウィンドウで右クリック
2. Create > Sound > SoundVariantDictionary
3. カテゴリ名を設定
4. Variant Itemsにアイテムを追加
5. 各アイテムにVariantsを追加

### 基本的な使用
```csharp
[SerializeField]
private SoundVariantDictionary variantDictionary;

// インデックス指定で取得
var clip = variantDictionary.GetClipByIndex("footstep", 0);
audioSource.PlayOneShot(clip);

// バリエーション数を取得
int count = variantDictionary.GetVariantCount("footstep");
Debug.Log($"バリエーション数: {count}");

// 最初の有効なクリップを取得
var firstClip = variantDictionary.GetFirstClip("footstep");

// アイテム情報を取得
if (variantDictionary.TryGetVariantItem("footstep", out var item))
{
    Debug.Log($"バリエーション数: {item.VariantCount}");
    Debug.Log($"有効なバリエーション数: {item.ValidVariantCount}");

    // ラベルで取得
    var walkClip = item.GetClipByLabel("walk");
}
```

### 連番再生の例
```csharp
private int currentIndex = 0;

void PlayNextVariant(string key)
{
    int count = variantDictionary.GetVariantCount(key);
    if (count == 0) return;

    var clip = variantDictionary.GetClipByIndex(key, currentIndex);
    if (clip != null)
    {
        audioSource.PlayOneShot(clip);
    }

    currentIndex = (currentIndex + 1) % count;
}
```

## データ設計例

### 足音バリエーション
| Key | Variants | 説明 |
|-----|----------|------|
| footstep_grass | grass_01, grass_02, grass_03 | 草の上の足音 |
| footstep_stone | stone_01, stone_02 | 石の上の足音 |
| footstep_wood | wood_01, wood_02, wood_03, wood_04 | 木の上の足音 |

### 攻撃音バリエーション
| Key | Variants | 説明 |
|-----|----------|------|
| attack_combo | combo_1, combo_2, combo_3, combo_finish | コンボ攻撃音（順番再生用） |

## Inspector設定

### Dictionary Settings（継承）
| フィールド | 説明 |
|-----------|------|
| Category | カテゴリ名 |
| Sound Type | SE（固定推奨） |

### Variant Items
| フィールド | 説明 |
|-----------|------|
| Key | 再生キー |
| Variants | AudioClipとラベルのリスト |
| Group Id | グループID |
| Priority | 優先度 |

### Variants（各アイテム内）
| フィールド | 説明 |
|-----------|------|
| Clip | AudioClip |
| Label | ラベル（任意、識別用） |

## 設計方針
- **SoundDictionary継承**: 基本機能は継承元を利用
- **バリエーション対応**: 同一キーで複数音声を管理
- **インデックスアクセス**: 順番指定での再生に対応
- **ラベル検索**: 名前指定での検索に対応
- **互換性維持**: SoundManagerから利用可能

## 配置場所
- スクリプト: `unity/Assets/Scripts/System/Sound/SoundVariantDictionary.cs`
- アセット: `unity/Assets/DataAsset/Sound/` 以下に配置

## 依存関係
- UnityEngine
- SoundDictionary.cs

## 注意事項
- **インデックス範囲**: GetClipByIndex()で範囲外のインデックスを指定するとnullを返す
- **空リスト**: Variantsが空の場合、GetFirstClip()やGetClipByIndex()はnullを返す
- **ラベル重複**: 同一アイテム内でラベルが重複した場合、最初に見つかったものを返す
