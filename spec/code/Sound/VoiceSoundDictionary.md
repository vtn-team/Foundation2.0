# VoiceSoundDictionary

## 概要
SoundDictionaryを継承したボイス専用のサウンドディクショナリ。
再生キー1つに対して複数のAudioClipバリエーションを設定でき、重みづけ確率によってランダムに選択される。
同じセリフでも複数の音声パターンを用意することで、自然な演出が可能。

## 責務
- 再生キーに対する複数AudioClipのバリエーション管理
- 重みづけ確率によるAudioClip選択
- SoundDictionaryとの互換性維持（SoundManagerから利用可能）

## データ構造

### VoiceClipVariation（AudioClipバリエーション）
| パラメータ | 型 | 説明 |
|-----------|-----|------|
| Clip | AudioClip | 音声ファイル |
| Weight | int | 出現率（重み、デフォルト1） |

### VoiceSoundDictionaryItem（ボイスアイテム）
| パラメータ | 型 | 説明 |
|-----------|-----|------|
| Key | string | 再生キー（一意の識別子） |
| Variations | List&lt;VoiceClipVariation&gt; | バリエーションリスト |
| GroupId | int | グループID |
| Priority | int | 優先度 |

## 重みづけ確率計算

重みの合計に対する各クリップの割合で確率が決定される。

**計算式:**
```
確率 = 個別の重み / 全体の重みの合計
```

**例:**
| Clip | Weight | 計算 | 確率 |
|------|--------|------|------|
| voice_A.wav | 3 | 3/(3+1+1) | 60% |
| voice_B.wav | 1 | 1/(3+1+1) | 20% |
| voice_C.wav | 1 | 1/(3+1+1) | 20% |

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
    public class VoiceClipVariation
    {
        [SerializeField]
        [Tooltip("音声ファイル")]
        private AudioClip clip;

        [SerializeField]
        [Tooltip("出現率（重み）")]
        private int weight = 1;

        /// <summary>
        /// AudioClip
        /// </summary>
        public AudioClip Clip => clip;

        /// <summary>
        /// 重み（出現率）
        /// </summary>
        public int Weight => weight;

        /// <summary>
        /// 有効かどうか
        /// </summary>
        public bool IsValid => clip != null && weight > 0;
    }

    /// <summary>
    /// ボイス用サウンドアイテム
    /// </summary>
    [Serializable]
    public class VoiceSoundDictionaryItem
    {
        [Header("Identification")]
        [SerializeField]
        [Tooltip("再生キー（一意の識別子）")]
        private string key;

        [Header("Variations")]
        [SerializeField]
        [Tooltip("AudioClipバリエーションリスト")]
        private List<VoiceClipVariation> variations = new List<VoiceClipVariation>();

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
        public IReadOnlyList<VoiceClipVariation> Variations => variations;

        /// <summary>
        /// グループID
        /// </summary>
        public int GroupId => groupId;

        /// <summary>
        /// 優先度
        /// </summary>
        public int Priority => priority;

        /// <summary>
        /// バリエーション数
        /// </summary>
        public int VariationCount => variations.Count;

        /// <summary>
        /// 有効かどうか
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(key) && variations.Count > 0;

        /// <summary>
        /// 重みの合計
        /// </summary>
        public int TotalWeight
        {
            get
            {
                int total = 0;
                foreach (var v in variations)
                {
                    if (v.IsValid) total += v.Weight;
                }
                return total;
            }
        }

        /// <summary>
        /// 重みづけ確率でランダムにAudioClipを取得
        /// </summary>
        /// <returns>選択されたAudioClip</returns>
        public AudioClip GetRandomClip()
        {
            if (variations.Count == 0) return null;

            // バリエーションが1つの場合、有効なら返す
            if (variations.Count == 1)
            {
                return variations[0].IsValid ? variations[0].Clip : null;
            }

            int totalWeight = TotalWeight;

            // 有効な重みがない場合、最初の有効なバリエーションを返す
            if (totalWeight <= 0)
            {
                return GetFirstValidClip();
            }

            int random = UnityEngine.Random.Range(0, totalWeight);
            int cumulative = 0;

            foreach (var variation in variations)
            {
                if (!variation.IsValid) continue;

                cumulative += variation.Weight;
                if (random < cumulative)
                {
                    return variation.Clip;
                }
            }

            // フォールバック: 最後の有効なバリエーションを返す
            return GetLastValidClip();
        }

        /// <summary>
        /// 最初の有効なAudioClipを取得
        /// </summary>
        private AudioClip GetFirstValidClip()
        {
            foreach (var variation in variations)
            {
                if (variation.IsValid) return variation.Clip;
            }
            return null;
        }

        /// <summary>
        /// 最後の有効なAudioClipを取得
        /// </summary>
        private AudioClip GetLastValidClip()
        {
            for (int i = variations.Count - 1; i >= 0; i--)
            {
                if (variations[i].IsValid) return variations[i].Clip;
            }
            return null;
        }

        /// <summary>
        /// 指定インデックスのAudioClipを取得
        /// </summary>
        public AudioClip GetClipByIndex(int index)
        {
            if (index >= 0 && index < variations.Count)
            {
                return variations[index].Clip;
            }
            return null;
        }

        /// <summary>
        /// SoundDictionaryItem互換のアイテムを生成
        /// </summary>
        public SoundDictionaryItem ToSoundDictionaryItem()
        {
            // 互換性のためにランダム選択したClipで生成
            // 注: SoundDictionaryItemはprivateフィールドのため、実行時に動的生成は困難
            // SoundManagerとの連携はVoiceSoundDictionary経由で行う
            return null;
        }
    }

    /// <summary>
    /// ボイス用サウンドディクショナリ
    /// </summary>
    [CreateAssetMenu(fileName = "VoiceSoundDictionary", menuName = "Sound/VoiceSoundDictionary")]
    public class VoiceSoundDictionary : SoundDictionary
    {
        [Header("Voice Items")]
        [SerializeField]
        [Tooltip("ボイスアイテムリスト")]
        private List<VoiceSoundDictionaryItem> voiceItems = new List<VoiceSoundDictionaryItem>();

        // ボイスアイテムのキャッシュ
        private Dictionary<string, VoiceSoundDictionaryItem> voiceItemMap;

        /// <summary>
        /// ボイスアイテムリスト
        /// </summary>
        public IReadOnlyList<VoiceSoundDictionaryItem> VoiceItems => voiceItems;

        /// <summary>
        /// 初期化（オーバーライド）
        /// </summary>
        public override void Initialize()
        {
            if (isInitialized) return;

            // 基底クラスの初期化
            base.Initialize();

            // ボイスアイテムマップの初期化
            voiceItemMap = new Dictionary<string, VoiceSoundDictionaryItem>();

            foreach (var item in voiceItems)
            {
                if (!item.IsValid) continue;

                if (!voiceItemMap.ContainsKey(item.Key))
                {
                    voiceItemMap[item.Key] = item;
                }
            }
        }

        /// <summary>
        /// ボイスアイテムを取得
        /// </summary>
        public VoiceSoundDictionaryItem GetVoiceItem(string key)
        {
            if (!isInitialized) Initialize();

            voiceItemMap.TryGetValue(key, out var item);
            return item;
        }

        /// <summary>
        /// ボイスアイテムを取得（Try形式）
        /// </summary>
        public bool TryGetVoiceItem(string key, out VoiceSoundDictionaryItem item)
        {
            if (!isInitialized) Initialize();

            return voiceItemMap.TryGetValue(key, out item);
        }

        /// <summary>
        /// 重みづけ確率でランダムにAudioClipを取得
        /// </summary>
        public AudioClip GetRandomClip(string key)
        {
            if (TryGetVoiceItem(key, out var item))
            {
                return item.GetRandomClip();
            }
            return null;
        }

        /// <summary>
        /// ボイスキーが存在するか確認
        /// </summary>
        public bool ContainsVoiceKey(string key)
        {
            if (!isInitialized) Initialize();

            return voiceItemMap.ContainsKey(key);
        }
    }
}
```

## 使用例

### ScriptableObjectの作成
1. Projectウィンドウで右クリック
2. Create > Sound > VoiceSoundDictionary
3. カテゴリ名を設定
4. Voice Itemsにボイスアイテムを追加
5. 各アイテムにVariationsを追加し、重みを設定

### 基本的な使用
```csharp
[SerializeField]
private VoiceSoundDictionary voiceDictionary;

// ランダム選択で再生
var clip = voiceDictionary.GetRandomClip("greeting_001");
audioSource.PlayOneShot(clip);

// アイテム取得
if (voiceDictionary.TryGetVoiceItem("greeting_001", out var item))
{
    Debug.Log($"バリエーション数: {item.VariationCount}");
    Debug.Log($"重み合計: {item.TotalWeight}");
}
```

### SoundPlayerとの連携
```csharp
// SoundManagerに登録
SoundManager.Instance.AddSoundDictionary(voiceDictionary);

// VoiceSoundDictionary専用の再生処理
public void PlayVoice(string key)
{
    var clip = voiceDictionary.GetRandomClip(key);
    if (clip != null)
    {
        // 直接AudioSourceで再生、またはSoundPlayer経由
        voiceAudioSource.PlayOneShot(clip);
    }
}
```

## データ設計例

### キャラクターボイス用
| Key | Variations | 説明 |
|-----|------------|------|
| char_attack_01 | attack_a(3), attack_b(1), attack_c(1) | 攻撃ボイス（Aが60%で多め） |
| char_damage_01 | damage_a(1), damage_b(1) | ダメージボイス（均等） |
| char_greeting | greet_a(2), greet_b(2), greet_c(1) | 挨拶（A,Bが40%、Cが20%） |

## Inspector設定

### Dictionary Settings（継承）
| フィールド | 説明 |
|-----------|------|
| Category | カテゴリ名 |
| Sound Type | Voice（固定推奨） |

### Voice Items
| フィールド | 説明 |
|-----------|------|
| Key | 再生キー |
| Variations | AudioClipと重みのリスト |
| Group Id | グループID |
| Priority | 優先度 |

### Variations（各アイテム内）
| フィールド | 説明 |
|-----------|------|
| Clip | AudioClip |
| Weight | 出現率（整数値、デフォルト1） |

## 設計方針
- **SoundDictionary継承**: 基本機能は継承元を利用
- **バリエーション対応**: 同一キーで複数音声を管理
- **重みづけ確率**: 柔軟な出現率制御
- **互換性維持**: SoundManagerから利用可能

## 配置場所
- スクリプト: `unity/Assets/Scripts/System/Sound/VoiceSoundDictionary.cs`
- アセット: `unity/Assets/DataAsset/Sound/Voice/` 以下に配置

## 依存関係
- UnityEngine
- SoundDictionary.cs

## 注意事項
- **SoundType設定**: Voice Dictionaryでは`SoundType.Voice`を設定推奨
- **重み0禁止**: Weight=0のバリエーションは選択されない
- **空リスト**: Variationsが空の場合、GetRandomClipはnullを返す
