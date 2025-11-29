# PrefabStockクラス設計

## 実装
- Pure Classである
- シングルトンとして表現される
- MonoBehaviourは継承しない
- staticでシーン移動やエディタ再生停止でもプールは破棄しない


# 概要
- ゲーム中すべてのPrefabはこのクラスからアクセスする
- ゲーム中に使用するPrefabのアクセスをまとめる
- 自前のObjectPoolで各Prefabの生成を管理する
- PrefabへのDIを行う
	- Prefab生成時に必要なスクリプトがあればつける
- 将来的な実装
	- DLC対応
		- Addressablesからの読み込みをサポート


# IObjectPoolインターフェース
PrefabStockで管理されるオブジェクトはIObjectPoolインターフェースを実装する。

## メソッド
| メソッド | 説明 |
|---------|------|
| `OnPoolInstantiate(string prefabKey)` | オブジェクト生成時に1回呼ばれる |
| `OnPoolUse()` | プールから取り出される度に呼ばれる |
| `OnPoolRelease()` | プールに返却される度に呼ばれる |
| `OnPoolDestroy()` | オブジェクト破棄時に呼ばれる |

## 実装クラス
以下の基底クラスにvirtual実装がある。派生クラスでoverrideして処理を追加可能。

| クラス | 説明 |
|-------|------|
| `EnemyBase` | 敵の基底クラス（Kuronoir, Usagi, Octopus等） |
| `BasicEffect` | エフェクトクラス |
| `Knife` | 投擲物 |
| `AppleBomb` | 爆発物 |


# 変数
- prefabDic: PrefabDictionary
- objectPools: Dictionary<string, ObjectPoolData>
- poolRootObject: GameObject（DontDestroyOnLoad対象）
- isInitialized: bool


# 処理フロー

## 生成時
1. prefabDicを読み込む
	1. prefabDicは`Assets/DataAsset/PrefabDictionary.asset`にある
2. ObjectPoolの準備をする
	1. エディタ再生時は準備をしない

## InitialLoad
ゲーム開始時に呼び出し、全プレファブを事前生成する。

1. プールルートオブジェクトを作成（DontDestroyOnLoad）
2. PrefabDictionaryの全キーに対してプールを準備
3. 各キーのlimit数までオブジェクトを事前生成
4. 生成時にIObjectPool.OnPoolInstantiateを呼び出す

## Prefabアクセス
- `PrefabReference`: Prefabの参照を取得（プール不使用）
- `CreateInstance`: プールから空いているインスタンスを返す
	- IObjectPool.OnPoolUseを呼び出す
	- 未使用がなくlimit未満なら新規作成
- `ReleaseInstance`: インスタンスをプールに返却
	- IObjectPool.OnPoolReleaseを呼び出す
- `CreateInstanceEditor`: エディタ拡張時に使用。普通にInstantiateする。
- `ClearAllPools`: 全プールをクリア（通常は使用しない）


# プール管理の特徴

## シーン移動での永続化
- プールルートオブジェクトにDontDestroyOnLoadを適用
- シーン移動してもプールされたオブジェクトは破棄されない

## エディタ再生停止での永続化（エディタのみ）
- プールオブジェクトにHideFlags.DontSaveを設定
- エディタで再生停止しても即座に破棄されない
- 次回再生時にstaticインスタンスはリセットされる


# PrefabDictionary
| 項目 | 説明 |
|-----|------|
| keyName | プレファブを識別するキー |
| prefab | Prefabの参照 |
| limit | 生成上限値（プールサイズ） |
