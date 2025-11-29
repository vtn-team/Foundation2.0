# QuantizeTestクラス設計


# 概要
- SoundQuantizerクラスのテストをする

# 実装
- MonoBehaviourを継承する
- SoundQuantizerのクラス設定ができるようにする
- クリックした際のイベントをクォンタイズする

# 外部インタフェース
- ClickEvent: クリックする。クォンタイズされてイベントが発火する

# 処理フロー
- 初期化時にSoundQuantizerのコールバックを登録

# 期待値
- なし

# エッジケース
- なし