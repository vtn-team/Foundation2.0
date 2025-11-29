# FPSRenderer


# 概要
- ゲームのFPSを計測して、Canvas上に表示する


# 処理
- FPS(Frame Per Second)を計測する
- 表示フラグを内包し、GameManagerのIsDebugがONの場合のみ表示をする
- 計測した数値を内部変数に保存しておき、それをCanvasに表示する
	- 表示はTextMeshProのテキストを取得して行う。


