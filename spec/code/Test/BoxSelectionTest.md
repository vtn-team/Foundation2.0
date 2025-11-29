# BoxSelectionTestクラス設計


# 概要
- BoxSelectionクラスのテストをする

# 実装
- MonoBehaviourを継承する
- BoxSelectionに渡すBoxSelectionSheetを設定できるようにする
- N回の試行テストができるようにする


# 内部変数
- boxSelect: BoxSelectionクラス
- sheet: BoxSelectionSheet
- drawNum: DrawManyで何回試行するか

# 外部インタフェース
- DrawOne: 1回テスト。何が出たかをログで表示
- DrawMany: N回テスト。何が何回出たかをサマリでログで表示

# 処理フロー
- 初期化時にsheetをBoxSelectionに渡してセットアップする

# 期待値
- なし

# エッジケース
- なし