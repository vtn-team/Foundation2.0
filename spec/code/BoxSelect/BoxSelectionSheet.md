# BoxSelectionSheetクラス設計


# 概要
- ボックスガチャ設定用のシート

# 実装
- ScriptableObjectを継承する
- BoxObjectDataのList配列をメンバーに持ち設定できるようにする
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
