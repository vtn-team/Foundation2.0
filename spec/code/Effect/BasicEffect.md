# BasicEffectクラス設計

## 実装
- MonoBehaviourを継承する
- IHitStopTargetを継承する
- このクラスを含んだPrefabがPrefabDictionaryに登録される
	- このクラスはPrefabのRootにある
- ワンショットエフェクトの場合、再生が終了すると自身を破棄してオブジェクトプールに戻す

# 概要
- 基本的なエフェクトの再生
- ヒットストップの影響を個別に対応できる


# 外部変数
- hitStopEnable: ヒットストップやヒットスローの影響を受けるかどうか
- playPos: 再生位置
- particleSystem: エフェクトのParticleSystemの参照


# 外部インタフェース
- Play: エフェクトを再生する。
- SetPosition: 再生位置を修正する。transform.positionを修正。
- Attach: 特定のノードの子供に置かれる

# 処理フロー
##  IHitStopTarget.OnTimeScaleUpdate
- 所持しているParticleSystemの再生スピードを調整する。
- hitStopEnableがfalseなら何もしない。


# # エディタ拡張
- 「登録」ボタン
	- このオブジェクトがPrefab化されているかを確認する
	- Prefabの名前と参照を取得し、PrefabDictionaryに登録する
		- PrefabKeyNameはPrefab名である
		- 重複している(既に登録済み)場合は何もしない

- タイムライン拡張対応
	- EffectPlayerから再生位置をもらってエフェクトの再生状態を合わせる
	- エフェクト再生終了時間をEffectPlayerに渡す