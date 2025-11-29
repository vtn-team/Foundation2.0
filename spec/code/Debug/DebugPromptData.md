# DebugPromptDataクラス設計

## 概要
- デバッグプロンプトの設定データを管理するクラス
- セーブデータとして永続化される
- シリアライズ可能な構造

## クラス構成

### DebugPromptData
デバッグ設定を保持するシリアライズ可能なデータクラス

## プロパティ

| プロパティ | 型 | デフォルト値 | 説明 |
|---|---|---|---|
| StopGameFrame | bool | false | デバッグメニュー表示中にゲームを一時停止するか |
| UseAnotherKeymap | bool | true | デバッグメニュー表示中にゲーム入力を無効化するか |

## メソッド

| メソッド | 戻り値 | 説明 |
|---|---|---|
| GetToggleState(string) | bool | トグルコマンドの状態を取得 |
| SetToggleState(string, bool) | void | トグルコマンドの状態を設定 |
| GetValueState(string, float) | float | 値コマンドの状態を取得 |
| SetValueState(string, float) | void | 値コマンドの状態を設定 |
| ResetToDefault() | void | 全ての設定をデフォルト値にリセット |

## 内部データ構造

### SerializableDictionary<TKey, TValue>
Unity標準のDictionaryはシリアライズできないため、独自実装のシリアライズ可能な辞書クラス

- `ISerializationCallbackReceiver`を実装
- `OnBeforeSerialize`でリストに変換
- `OnAfterDeserialize`で辞書を再構築

## シリアライズ構造

```csharp
[Serializable]
public class DebugPromptData
{
    [SerializeField] private bool stopGameFrame;
    [SerializeField] private bool useAnotherKeymap;
    [SerializeField] private SerializableDictionary<string, bool> toggleStates;
    [SerializeField] private SerializableDictionary<string, float> valueStates;
}
```

## 依存関係
- UnityEngine（シリアライズ用）

## ファイルパス
- `unity/Assets/Scripts/Debug/DebugPromptData.cs`
