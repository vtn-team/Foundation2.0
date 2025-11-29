# EffectPlayerクラス設計

## 実装
- Pure Classである
- あらゆるクラスに移譲される

# 概要
- ゲーム中すべてのエフェクト再生処理はこのクラスからアクセスする


# 外部変数
- quantizeFrame: クォンタイズするフレーム。デフォルトは0
- prefabKey: prefabDicのキー
- attachTarget: 再生時に対象オブジェクトの子供として生成する


# 外部インタフェース
- Play: エフェクトを再生する。引数はKey Stringとする。
- SetAttachTarget: アタッチ対象(attachTarget)を設定する


# 処理フロー

## 再生
1. PrefabStockからPrefabを取得する
	- クォンタイズされたヒットストップとするため、呼び出し時にクォンタイズする
		- 「拍に合わせたいフレーム」を設定し、その時間がクォンタイズされるように調整する
	1. BasicEffectで受け取り、再生処理を実行する
	2. attachTargetがあれば、PrefabをattachTargetの子供に設定する
		1. attachTargetが無ければ、BasicEffect経由で再生位置を設定する

# # タイムライン拡張
- タイムライン上で再生確認ができる実装を持つ(EffectPlayerPlayable)
	- トラックを配置したら、エフェクト再生終了時間までの長さのトラックを自動生成する
	- トラックの長さは変更できず、エフェクトが調整されたらリアルタイムにトラックの長さを変更する

# # エディタ拡張
- prefabKeyはPrefabDictionaryからキーを取得してPopupで設定できるようにする
	- BasicEffectがついているまたは継承しているクラス限定
- 「エフェクトを再生」ボタンを用意する
	1. PrefabDictionaryからPrefabを受け取る
	2. Prefabを新しくInstantiateする
	3. playPosの位置に置く
	4. IEffectでGetComponentする
	5. IEffectから再生処理を実行する