# SceneTransitionクラス設計

# 概要
- 画面遷移時に特殊効果をかける

## 実装
- MonoBehaviourを継承する
- 独自のCanvasを持つ
- シーン移行で破棄されない
- 無かったらPrefabインスタンスを生成する
	- Prefab/SceneTransitCanvas でAddressablesに登録されているので、これを読みだして生成する


# 外部インタフェース
## SceneInitTransition
- 前回の遷移パターンと対になる処理を実行する

## FadeIn
- シンプルなフェードで画面を黒くする
- 真っ黒のImageのalphaをシェーダでトランジションする
- 引数にフェード時間(Outも同じ時間)と、In時のイージングカーブとOut時のイージングカーブをもらう

## FadeOut
- シンプルなフェードを開ける
- 真っ黒のImageのalphaをシェーダでトランジションする

## ScreenIn
- 画面のキャプチャをとり、それをImageにはりつける

## ScreenCut
- 画面のキャプチャを真っ二つにして次のシーンにうつる

## BurningFadeIn
- 画面のキャプチャをとり、それをImageにはりつける
- 画面がちょっとくすぶっている

## BurningFadeOut
- 燃えるシェーダで画面を開ける