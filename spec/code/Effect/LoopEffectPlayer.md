# LoopEffectPlayerクラス設計

## 概要
- ループエフェクト再生を管理するクラス
- Play()で再生開始、PlayOut()で終了アニメーションを再生して終了する
- EffectPlayerを拡張し、ループ再生とフェードアウト機能を持つ

## 実装
- Pure Classとしてシリアライズ可能
- BasicEffectを使用してエフェクトを再生

## 外部変数(SerializeField)

| プロパティ         | 型         | 説明                  |
| ------------- | --------- | ------------------- |
| prefabKeyIn   | string    | PrefabDictionaryのキー |
| prefabKeyLoop | string    | PrefabDictionaryのキー |
| prefabKeyOut  | string    | PrefabDictionaryのキー |
| attachTarget  | Transform | エフェクトをアタッチする対象      |

## 内部変数

| 変数            | 型           | 説明                |
| ------------- | ----------- | ----------------- |
| currentEffect | BasicEffect | 現在再生中のエフェクトインスタンス |
| isPlaying     | bool        | 再生状態フラグ           |

## プロパティ

| プロパティ        | 型         | 説明          |
| ------------ | --------- | ----------- |
| PrefabKey    | string    | プレハブキーのアクセサ |
| AttachTarget | Transform | アタッチ対象のアクセサ |
| IsPlaying    | bool      | 再生中かどうか     |

## メソッド

| メソッド                       | 戻り値  | 説明                     |
| -------------------------- | ---- | ---------------------- |
| SetAttachTarget(Transform) | void | アタッチ対象を設定する            |
| Play()                     | void | ループエフェクトを再生開始する        |
| PlayOut()                  | void | ループエフェクトを終了する（フェードアウト） |
| ForceStop()                | void | 強制的にエフェクトを停止する         |

## 処理フロー

### Play()
1. 既に再生中の場合は何もしない
2. PrefabStockからPrefabを取得(prefabKeyIn)
3. BasicEffectコンポーネントを取得
4. attachTargetがあればアタッチ
5. エフェクトを再生
6. isPlayingをtrueに設定
7. prefabKeyLoopが設定されている場合、prefabKeyInの再生終了時間にPlayForceをprefabKeyLoopを引数にして呼び出す

### PlayForce()
1. 引数としてprefabKeyをもらう
2. PrefabStockからPrefabを取得(引数のprefabKey)
3. BasicEffectコンポーネントを取得
4. attachTargetがあればアタッチ
5. エフェクトを再生

### PlayOut()
1. 再生中でない場合は何もしない
2. PlayForce()をprefabKeyOutで呼び出す
3. currentEffectをnullに設定
4. isPlayingをfalseに設定

## 依存関係
- PrefabStock
- BasicEffect

## ファイルパス
- `unity/Assets/Scripts/Effect/LoopEffectPlayer.cs`
