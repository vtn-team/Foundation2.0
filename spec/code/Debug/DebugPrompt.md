# DebugPromptクラス設計

## 概要
- ゲーム中のデバッグコマンドをゲームプレイ中に表現する
- Pure ClassとしてGameManagerが所持する
- デバッグ機能の中核となるコントローラークラス

## 実装
- Pure Classであり、GameManagerが所持する
- 子供にDebugPromptDataとDebugPromptViewを持つ
    - DebugPromptDataはセーブデータとして残る
    - DebugPromptViewは生成時に作られる
- DebugPromptはリリースビルド時は生成されない（`#if UNITY_EDITOR || DEVELOPMENT_BUILD`）
- デバッグ機能はDebugCommandを踏襲するコマンドパターンで用意される
- ゲーム中に実行するDebugCommandのリストを持つ

## 処理フロー
1. L2トリガー（Gamepad）またはLeft Ctrl（Keyboard）を押し続けている間、デバッグメニューを表示する
    1. Stop GameFrameが有効な場合、ゲームの進行を停止する（ポーズと同様）
    2. Another Keymapが有効な場合、デバッグメニューが開いている間は、ゲーム中の操作を無効にする
	3. キーボードの場合は1～9を設定されているデバッグコマンドにアサインし、押したら機能させる
		1. UIにその旨を表示する
2. それぞれのメニューで実行されたデバッグ処理を呼び出す
3. キーを離すとメニューを閉じ、ゲーム状態を復元する
