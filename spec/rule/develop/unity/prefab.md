# ルール
- ゲーム中に動的に生成されるPrefabは、すべてPrefabStockを経由し生成される
	- ゲーム中生成されるオブジェクトはそれぞれ生成上限数を持つ
	- 生成上限数に接した場合、PrefabStockが上限を超えてオブジェクトを生成する
		- 同時に「○○のキャラの生成上限を超えました」というエラーも出力する

- PrefabデータはすべてPrefabDictionaryクラスに登録されている
	- PrefabStockは`/unity/Assets/DataAsset/PrefabDictionary.asset`をAwakeで読み込む


# 生成について
- Prefanと対応するKeyNameを渡すことで生成を行う
- 直接のInstantiateはしない