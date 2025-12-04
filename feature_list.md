# 機能リスト
検証済みかどうかをメモっておくとよい。

## クォンタイズエンジン(未検証)
再生タイミングをいい感じに調整できる。ピークタイミングを拍に合わせる調整弁つき。  
コード： [[Quantizer]]  

## オーディオスペクトラム(未検証)
別件で作ったので応用したい。  
もってくる必要あり。伴い仕様もまだない。  

## シンプルなDI(使用中)
パラメータを外部シートから注入する仕組み。  
コードも自動生成するので使用者はInjectアトリビュートを書くだけ。めっちゃ楽にした。  
アセンブリ参照が必要なものでバグるので対応中。  
[[InjectSystem]]
[[inject_rule]]

## ブラックボード(未検証)
機能が増すごとに複雑化する値の参照を一元管理してクレバーにやれないかと考えた仕組み。  
ブラックボードアーキテクチャの考え方を流用して、黒板にシングルトン的に値を書いていき、それを書く実装が参照する。  
順番解決やロックの仕組みは、ブラックボードが担当する。(未対応)  
仕様が空なので、どこかで作成する。  
[[Blackboard|Blackboard]]

## Prefab辞書(使用中)
オブジェクトプールを管理しやすくするよう、Prefabとそれを参照するStagePaletteという仕組みに分解している。  
PrefabDictionaryは、任意のタイミングでオブジェクトプールを作成する。  
各処理で返却の対応がうまくできていない。  
[[PrefabDictionary]]

## SE再生処理(使用中)
オーディオもオブジェクトプールみたいなのを用意した。
ちょっと微妙かもしれない。
[[SoundEffectPlayer]]
[[SoundSourceDictionary]]

## エフェクト再生システム(未検証)
パーティクルエフェクトの再生を管理する仕組み。
BasicEffectがエフェクト本体、EffectPlayerがPure Classとして再生制御を担当。
LoopEffectPlayerはイン・ループ・アウトの3フェーズエフェクトに対応。
ヒットストップ対応のためIHitStopTargetを実装。
[[BasicEffect]]
[[EffectPlayer]]
[[LoopEffectPlayer]]
[[EffectPlayerComponent]]

## ワールドタイムスケール/ヒットストップ(未検証)
ゲーム中の時間制御を管理する仕組み。
ヒットストップ（完全停止）とヒットスロー（イージング付きスロー）に対応。
Quantizerと連携してクォンタイズされたタイミングで発動可能。
Timelineトラック拡張でエディタプレビューに対応。
[[WorldTimeComposer]]
[[ITimeScaleTarget]]
[[HitStopTrack]]
[[HitSlowTrack]]

## キューシステム(未検証)
演出シーケンスをタイムラインから自動生成する仕組み。
CueTimelineDirectorでタイムラインを解析し、CueSequence派生クラスを自動生成。
SE/Voice/Effect/HitStop/HitSlowのトラックに対応。
[[CueOrchestrator]]
[[CueSequence]]
[[CueTimelineDirector]]
[[SoundEffectTrack]]
[[VoiceTrack]]
[[EffectPlayerTrack]]

## ボックスガチャ(未検証)
重みづけ抽選を行うボックスガチャの仕組み。
一度排出されたものはリセットするまで抽選されない。
シード値による再現性を確保。
[[BoxSelection]]
[[BoxSelectionSheet]]

## デバッグプロンプト(未検証)
ゲーム中のデバッグコマンドを管理・実行する仕組み。
L2トリガーまたはLeft Ctrlでメニュー表示。
UI Toolkitでオーバーレイ表示、1-9キーでコマンド実行。
リリースビルドには含まれない。
[[DebugPrompt]]
[[DebugPromptView]]
[[IDebugCommand]]

## モーションコントローラ(未検証)
Animatorのラッパークラス。
アニメーション遷移の制御を一元化。
将来的なIK対応を見据えた設計。
[[MotionController]]

## シーン遷移エフェクト(未検証)
画面遷移時の特殊効果を管理する仕組み。
フェードイン/アウト、スクリーンカット、燃えるフェードに対応。
Addressablesからプレファブを読み込み、シーン間で破棄されない。
[[SceneTransition]]

## FPS表示(未検証)
ゲームのFPSを計測してCanvas上に表示するツール。
TextMeshProで表示、デバッグ時のみ有効。
[[FPSRenderer]]

## ゲームレポーター(未検証)
実行中の様々な情報を記録するレポータークラス。
リプレイ記録、エラーチェック、CPUスパイク検知、アロケーション検知、映像記録に対応。
記録終了時にreportフォルダに出力。リリースビルドには含まれない。
[[GameReporter]]

## テストクラス(未検証)
各機能のテスト用コンポーネント。
BoxSelectionTest: ボックスガチャの抽選テスト
QuantizeTest: クォンタイズのタイミングテスト
[[BoxSelectionTest]]
[[QuantizeTest]]

## Melpomene(未検証)
GitHub Issues連携デバッグツール（エディタ専用）。
シーン上のオブジェクトに紐づけたバグ報告や改善要望を管理する。
Alt+Ctrl+クリックでチケット作成、SceneViewにチケットをオーバーレイ表示。
Slack/Discord通知機能あり。
[[Melpomene]]
[[MelpomeneNotification]]