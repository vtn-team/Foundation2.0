# ITimeScaleTargetクラス設計

# 概要
- WorldTimeComposerから時間コールバックが来るので処理をする

# 実装
- interface
- 何かのクラスにくっついて処理される

# 処理フロー
- 処理自体はグローバルアクセスで発火される

## 登録(実装クラスの初期化時)
1. WorldTimeComposerの登録処理をコール

## 時間更新
- WorldTimeComposerから毎フレーム時間(タイムスケール)が渡される
- もらった時間を各種コンポーネントのTimeScaleに適用していく
