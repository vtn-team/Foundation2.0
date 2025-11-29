# SoundDictionary

## 概要
サウンドアセットの定義データを管理するScriptableObject。
カテゴリと再生タイプ（SE/ME/VOICE）を本体パラメータとして持ち、データ配列として再生キー、AudioClip、グループID、優先度の4パラメータを持つリストを保持する。
SoundManagerはこのデータを読み込み、キーからサウンドを特定して再生する。

## 責務
- サウンドアセットの定義と管理
- カテゴリ・再生タイプによる分類
- 再生キーによるサウンドの識別
- グループID・優先度による再生制御パラメータの提供

## データ構造

### 本体パラメータ
| パラメータ     | 型         | 説明                                         |
| --------- | --------- | ------------------------------------------ |
| Category  | string    | カテゴリ名（例: "Player", "Enemy", "Environment"） |
| SoundType | SoundType | 再生タイプ（SE / ME / Voice）                     |

### データ配列（SoundDictionaryItem）
| パラメータ     | 型         | 説明                    |
| --------- | --------- | --------------------- |
| Key       | string    | 再生キー（一意の識別子）          |
| AudioClip | AudioClip | 音声ファイル参照              |
| GroupId   | int       | グループID（同一グループは排他再生など） |
| Priority  | int       | 優先度（高いほど再生優先）         |

## クラス定義

```csharp
namespace BaseSystem.Sound.InGame
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// サウンドディクショナリアイテム
    /// </summary>
    [Serializable]
    public class SoundDictionaryItem
    {
        [Header("Identification")]
        [SerializeField]
        [Tooltip("再生キー（一意の識別子）")]
        private string key;

        [Header("Audio")]
        [SerializeField]
        [Tooltip("AudioClip参照")]
        private AudioClip audioClip;

        [Header("Control")]
        [SerializeField]
        [Tooltip("グループID（同一グループは排他制御など）")]
        private int groupId;

        [SerializeField]
        [Range(0, 100)]
        [Tooltip("優先度（高いほど再生優先）")]
        private int priority = 50;

        /// <summary>
        /// 再生キー
        /// </summary>
        public string Key => key;

        /// <summary>
        /// AudioClip
        /// </summary>
        public AudioClip AudioClip => audioClip;

        /// <summary>
        /// グループID
        /// </summary>
        public int GroupId => groupId;

        /// <summary>
        /// 優先度
        /// </summary>
        public int Priority => priority;

        /// <summary>
        /// サウンドタイプ（親のSoundDictionaryから参照）
        /// </summary>
        public SoundType SoundType { get; internal set; }

        /// <summary>
        /// カテゴリ（親のSoundDictionaryから参照）
        /// </summary>
        public string Category { get; internal set; }

        /// <summary>
        /// 有効なアイテムかどうか
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(key) && audioClip != null;

        /// <summary>
        /// AudioClipの長さ（秒）
        /// </summary>
        public float Length => audioClip != null ? audioClip.length : 0f;
    }

    /// <summary>
    /// サウンドディクショナリ（ScriptableObject）
    /// </summary>
    [CreateAssetMenu(fileName = "SoundDictionary", menuName = "Sound/SoundDictionary")]
    public class SoundDictionary : ScriptableObject
    {
        #region SerializeField
        [Header("Dictionary Settings")]
        [SerializeField]
        [Tooltip("カテゴリ名（例: Player, Enemy, UI）")]
        private string category = "Default";

        [SerializeField]
        [Tooltip("再生タイプ")]
        private SoundType soundType = SoundType.SE;

        [Header("Sound Items")]
        [SerializeField]
        [Tooltip("サウンドアイテムリスト")]
        private List<SoundDictionaryItem> items = new List<SoundDictionaryItem>();
        #endregion

        #region Properties
        /// <summary>
        /// カテゴリ名
        /// </summary>
        public string Category => category;

        /// <summary>
        /// 再生タイプ
        /// </summary>
        public SoundType SoundType => soundType;

        /// <summary>
        /// サウンドアイテムリスト
        /// </summary>
        public IReadOnlyList<SoundDictionaryItem> Items => items;

        /// <summary>
        /// アイテム数
        /// </summary>
        public int Count => items.Count;
        #endregion

        #region Private Fields
        // キーからアイテムへのマッピング（キャッシュ）
        private Dictionary<string, SoundDictionaryItem> itemMap;

        // グループIDからアイテムリストへのマッピング（キャッシュ）
        private Dictionary<int, List<SoundDictionaryItem>> groupMap;

        // 初期化済みフラグ
        private bool isInitialized;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            Initialize();
        }

        private void OnValidate()
        {
            // エディタで変更があった場合は再初期化
            isInitialized = false;
            Initialize();

            // 重複キーのチェック
            ValidateKeys();
        }
        #endregion

        #region Initialization
        /// <summary>
        /// 初期化
        /// </summary>
        public void Initialize()
        {
            if (isInitialized) return;

            itemMap = new Dictionary<string, SoundDictionaryItem>();
            groupMap = new Dictionary<int, List<SoundDictionaryItem>>();

            foreach (var item in items)
            {
                if (!item.IsValid) continue;

                // 親情報を設定
                item.SoundType = soundType;
                item.Category = category;

                // キーマップに追加
                if (!itemMap.ContainsKey(item.Key))
                {
                    itemMap[item.Key] = item;
                }

                // グループマップに追加
                if (!groupMap.ContainsKey(item.GroupId))
                {
                    groupMap[item.GroupId] = new List<SoundDictionaryItem>();
                }
                groupMap[item.GroupId].Add(item);
            }

            isInitialized = true;
        }

        /// <summary>
        /// キーの重複チェック
        /// </summary>
        private void ValidateKeys()
        {
            var keySet = new HashSet<string>();
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.Key)) continue;

                if (keySet.Contains(item.Key))
                {
                    Debug.LogWarning($"[SoundDictionary] 重複キー検出: {item.Key} in {name}");
                }
                else
                {
                    keySet.Add(item.Key);
                }
            }
        }
        #endregion

        #region Query Methods
        /// <summary>
        /// キーからアイテムを取得
        /// </summary>
        /// <param name="key">再生キー</param>
        /// <returns>SoundDictionaryItem（存在しない場合はnull）</returns>
        public SoundDictionaryItem GetItem(string key)
        {
            if (!isInitialized) Initialize();

            itemMap.TryGetValue(key, out var item);
            return item;
        }

        /// <summary>
        /// キーからアイテムを取得（Try形式）
        /// </summary>
        /// <param name="key">再生キー</param>
        /// <param name="item">出力アイテム</param>
        /// <returns>存在する場合はtrue</returns>
        public bool TryGetItem(string key, out SoundDictionaryItem item)
        {
            if (!isInitialized) Initialize();

            return itemMap.TryGetValue(key, out item);
        }

        /// <summary>
        /// キーが存在するかチェック
        /// </summary>
        /// <param name="key">再生キー</param>
        /// <returns>存在する場合はtrue</returns>
        public bool ContainsKey(string key)
        {
            if (!isInitialized) Initialize();

            return itemMap.ContainsKey(key);
        }

        /// <summary>
        /// グループIDからアイテムリストを取得
        /// </summary>
        /// <param name="groupId">グループID</param>
        /// <returns>同一グループのアイテムリスト</returns>
        public IReadOnlyList<SoundDictionaryItem> GetItemsByGroup(int groupId)
        {
            if (!isInitialized) Initialize();

            if (groupMap.TryGetValue(groupId, out var list))
            {
                return list;
            }
            return Array.Empty<SoundDictionaryItem>();
        }

        /// <summary>
        /// 全キーを取得
        /// </summary>
        /// <returns>キーの列挙</returns>
        public IEnumerable<string> GetAllKeys()
        {
            if (!isInitialized) Initialize();

            return itemMap.Keys;
        }

        /// <summary>
        /// 全グループIDを取得
        /// </summary>
        /// <returns>グループIDの列挙</returns>
        public IEnumerable<int> GetAllGroupIds()
        {
            if (!isInitialized) Initialize();

            return groupMap.Keys;
        }
        #endregion

        #region Editor Support
#if UNITY_EDITOR
        /// <summary>
        /// エディタ用：アイテムを追加
        /// </summary>
        public void AddItem(SoundDictionaryItem item)
        {
            items.Add(item);
            isInitialized = false;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// エディタ用：アイテムを削除
        /// </summary>
        public void RemoveItem(SoundDictionaryItem item)
        {
            items.Remove(item);
            isInitialized = false;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// エディタ用：全アイテムをクリア
        /// </summary>
        public void ClearItems()
        {
            items.Clear();
            isInitialized = false;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// エディタ用：インデックスでアイテムを取得
        /// </summary>
        public SoundDictionaryItem GetItemByIndex(int index)
        {
            if (index >= 0 && index < items.Count)
            {
                return items[index];
            }
            return null;
        }

        /// <summary>
        /// エディタ用：サンプル再生
        /// </summary>
        [ContextMenu("Play Sample (First Item)")]
        private void PlaySample()
        {
            if (items.Count > 0 && items[0].AudioClip != null)
            {
                var previewClip = items[0].AudioClip;
                // Unity Editorのプレビュー機能を使用
                var previewMethod = typeof(UnityEditor.AudioImporter).Assembly
                    .GetType("UnityEditor.AudioUtil")
                    .GetMethod("PlayPreviewClip",
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                        null,
                        new System.Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
                        null);

                previewMethod?.Invoke(null, new object[] { previewClip, 0, false });
            }
        }

        /// <summary>
        /// エディタ用：プレビュー停止
        /// </summary>
        [ContextMenu("Stop Preview")]
        private void StopPreview()
        {
            var stopMethod = typeof(UnityEditor.AudioImporter).Assembly
                .GetType("UnityEditor.AudioUtil")
                .GetMethod("StopAllPreviewClips",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

            stopMethod?.Invoke(null, null);
        }
#endif
        #endregion
    }
}
```

## 使用例

### ScriptableObjectの作成
1. Projectウィンドウで右クリック
2. Create > Sound > SoundDictionary
3. カテゴリ名と再生タイプを設定
4. アイテムリストにサウンドを追加

### SoundManagerでの読み込み
```csharp
// InspectorでSoundDictionaryをアサイン
[SerializeField]
private SoundDictionary[] soundDictionaries;

// SoundManagerが自動的に読み込み
// キーからサウンドを取得可能に
var soundItem = soundManager.GetSoundData("jump_se");
```

### キーによるサウンド取得
```csharp
// 直接SoundDictionaryから取得
if (soundDictionary.TryGetItem("jump_se", out var item))
{
    Debug.Log($"キー: {item.Key}");
    Debug.Log($"カテゴリ: {item.Category}");
    Debug.Log($"タイプ: {item.SoundType}");
    Debug.Log($"グループ: {item.GroupId}");
    Debug.Log($"優先度: {item.Priority}");
    Debug.Log($"長さ: {item.Length}秒");
}
```

### グループによる取得
```csharp
// 同じグループIDのサウンドを全て取得
var footstepSounds = soundDictionary.GetItemsByGroup(groupId: 1);

// ランダム再生などに活用
var randomSound = footstepSounds[Random.Range(0, footstepSounds.Count)];
```

## データ設計例

### Player用SoundDictionary
| Key | AudioClip | GroupId | Priority |
|-----|-----------|---------|----------|
| player_jump | jump.wav | 0 | 50 |
| player_land | land.wav | 0 | 50 |
| player_attack | attack.wav | 1 | 60 |
| player_damage | damage.wav | 2 | 80 |
| player_death | death.wav | 2 | 100 |

### Environment用SoundDictionary
| Key | AudioClip | GroupId | Priority |
|-----|-----------|---------|----------|
| wind_loop | wind.wav | 10 | 20 |
| rain_loop | rain.wav | 10 | 20 |
| thunder | thunder.wav | 11 | 40 |

## Inspector設定

### Dictionary Settings
| フィールド | 説明 |
|-----------|------|
| Category | カテゴリ名（用途別の分類） |
| Sound Type | SE / ME / Voice の選択 |

### Sound Items（配列要素）
| フィールド | 説明 |
|-----------|------|
| Key | 一意の再生キー |
| Audio Clip | 音声ファイル |
| Group Id | グループID（排他制御などに使用） |
| Priority | 優先度（0〜100、高いほど優先） |

## 設計方針
- **ScriptableObject**: アセットとして管理、再利用可能
- **キーベース**: 文字列キーで直感的にアクセス
- **グループ分類**: 同種サウンドのグルーピングをサポート
- **優先度制御**: チャンネル競合時の優先順位を定義
- **キャッシュ**: Dictionary変換でO(1)アクセス

## 配置場所
- スクリプト: `unity/Assets/Scripts/System/Sound/SoundDictionary.cs`
- アセット: `unity/Assets/DataAsset/Sound/` 以下に用途別で配置

## 依存関係
- UnityEngine
- SoundType（SoundManager.csで定義）

## 注意事項
- **キー重複禁止**: 同一SoundDictionary内でキーの重複は警告
- **複数Dictionary対応**: SoundManagerは複数のSoundDictionaryをマージ
- **遅延初期化**: 初回アクセス時に内部マップを構築
- **エディタ変更時**: OnValidateで自動的に再初期化
