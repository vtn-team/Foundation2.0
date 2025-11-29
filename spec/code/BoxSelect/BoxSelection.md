# BoxSelectionクラス設計

# 概要
- 特定のオブジェクト集合から重みづけ抽選をする。つまりボックスガチャ。

# 実装
- BoxSelectionSheetから重みづけをもとに抽選を行う
- 一度排出されたものはリセットするまで抽選されない(ボックスガチャ)
- 内部乱数としてMathクラスの乱数を使用し、これにシードを与えて初期化する(再現性を作る)


# 処理フロー

## 初期化
- BoxSelectionSheetを内部クラスに実態として展開する(これをボックスとする)
- ランダムをシード値と合わせて初期化する

## Pop
- boxListから重みづけ確率で1つ選んで採る


# 内部変数
- boxList: BoxSelectObjectDataの配列。BoxSelectionSheetのnumをすべて展開した情報。

# 外部変数
- boxSheet: BoxSelectionSheetの情報
- randomSeed: シード値


# 外部インタフェース
- Pop: 何か1つを選択してとる
- ReturnObject: オブジェクトをボックスリストに戻す


# 期待値
- なし

# エッジケース
- なし


# BoxSelectionSheet
- ScriptableObjectを継承する
- BoxObjectDataのList配列。
- すべてpublicでよい。

## BoxObjectData
- prop: 重みづけ確率
- prefabKey: 対象物のPrefabKey
- num: 何個ボックスに入れておくか

## BoxSelectObjectData
- id: 管理ID。最初に連番で振られる
- prop: 重みづけ確率
- prefabKey: 対象物のPrefabKey
- inStock: このオブジェクトがBOX内にいる場合はtrue、排出された場合はfalseになる
