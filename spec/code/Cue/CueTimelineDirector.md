# CueTimelineDirectorrクラス設計

# 概要
- CueOrchestratorへわたすコマンドを生成するためのツール

# 必要要件
- PlayableDirectorを継承する


# 実装
- 対象物に対しての演出を作成する
- 「シーケンスを保存」ボタンを押すと、タイムラインの時間に基づきCueSequenceを自動生成する


# 外部インタフェース
- PlayEffectTask: コマンドのenumとコールバックを引数に取り、指定されたCommandを再生する
- PlayEffectTaskAsync: async関数として定義する。コマンドのenumを引数に取り、指定されたCommandを再生する


# エディタ拡張 / ツールサポート
- 以下の処理を実行し、CueSequenceの派生クラスを自動生成する
	- タイムラインを解析し、それぞれのトラックに必要なクラス/GameObjectを解析する
		- トラックの解析と実行する処理(イベント)は「トラックの解析」を参考にする
	- 自動生成されるCueSequence派生クラスの名前は、処理しているPlayableAssetの名前+CueSequenceとする
	- 自動生成されるCueSequenceで実装すべき内容については、[[CueSequence]]を参照する。
	- 自動生成する場所は、`Scripts/InGame/GeneratedCue/`とする
	- Setup関数は、宣言されている参照に必要な情報を引数ですべてもらい代入する形とする。
- トラックに設定されるアセット/ビヘイビアは全て、設定値としてクォンタイズ用のビート(デフォルト16)を設定できるようにする
	- CueTimelineDirectorにはBPMを設定でき、ビートタイミングと一致しないキューには警告を出す
	- 警告は各ビヘイビアに表示される
- キューを作成する際に無視するノードをCueTimelineDirectorのインスペクタ拡張で指定できる
	- 指定は「タイムライン名」に対して、トラック名を複数のstringのリストで持てるようにする形式で行う
	- これはリザルトなどのシステムで表示するものを出力させないようにするため

## トラックの解析（実装完了）
想定されるのは以下のトラックとなる。ルールに従い処理する。

### GameObject
- 演出対象物であると仮定し、参照を宣言する。処理はない。
- 実装: `private GameObject targetObject;`

### AnimationTrack
1. 対象のオブジェクトがMotionControllerを所持しているか確認
   - バインディング先のAnimatorがついているコンポーネントとその親に対して、MotionControllerの存在を確認する。
   - 存在する場合: `private MotionController motionController;`
   - 存在しない場合: `private Animator animator;`
1. MotionControllerを使用する場合、Animatorを参照し、再生予定のアニメーションが設定されているStateを確認する
	1. Stateの遷移条件を確認し、遷移条件にTriggerがあればそれを記録する
2. 再生処理:
   - MotionController所持時: `motionController.TriggerAnimation("{記録したトリガー名}");`
   - MotionController無し時: `animator.Play(clipName);`

### EffectPlayerTrack
- BasicEffectを直接アサインする形式
- EffectPlayerの参照を宣言
- BasicEffectの参照を宣言
	- ただしSetupではEffectPlayerの参照をもらわず、アタッチ先のターゲットをもらう
- 実装: 
  - `private EffectPlayer effectPlayer{Prefab名} = new EffectPlayer();`
  - BasicEffectが登録されているPrefabDictionaryのキーが存在すれば、SetupでeffectPlayerのprefabKeyに代入する
- 再生処理: 
  - effectPlayer.Play();

### HitStopTrack
- HitStopComposer.Instanceを使用（シングルトン）
- フィールド宣言不要
- 再生処理: `HitStopComposer.Instance.HitStop(duration);`

### HitSlowTrack
- HitStopComposer.Instanceを使用（シングルトン）
- フィールド宣言不要
- 再生処理: `HitStopComposer.Instance.HitSlow(duration, centerWeight, centerTimeScale, centerHoldTime, ease);`

### SoundEffectTrack
- **バインド不要** - Timeline側でSoundPlayerで再生する再生キー(string)を設定する
	- 再生キーはSoundManagerにアサインされているSoundType.SEに登録されているキーのリストを取得し、Popupで設定させる
- SoundPlayerの参照を宣言
  - `private SoundPlayer{アセット名} soundPlayer = new SoundPlayer(SoundType.SE);`
- 再生処理:
  - `soundPlayer.Play();`

### VoiceTrack
- **バインド不要** - Timeline側でSoundPlayerで再生する再生キー(string)を設定する
	- 再生キーはSoundManagerにアサインされているSoundType.Voiceに登録されているキーのリストを取得し、Popupで設定させる
- SoundPlayerの参照を宣言
  - `private SoundPlayer{アセット名} voicePlayer = new SoundPlayer(SoundType.Voice);`
- 再生処理:
  - `voicePlayer.Play();`
