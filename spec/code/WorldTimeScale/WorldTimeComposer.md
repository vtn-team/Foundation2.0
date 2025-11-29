# WorldTimeComposerクラス設計

# 概要
- ゲーム中の時間(TimeScale)を管理する

# 実装
- pure classである
- シングルトン

# 内部変数
- timeScaleTargets: ITimeScaleTargetを持つオブジェクトのリスト
- hitStopTime: ヒットストップ時間
- isHitSlow: ヒットスロー

# 処理フロー
- 処理自体はグローバルアクセスで発火される
- クォンタイズされたヒットストップとするため、呼び出し時にクォンタイズする
	- 停止時間をクォンタイズの拍に合わせるようにする

## Update
- GameManagerから呼び出される
- 以下のステップでタイムスケールを計算し、timeScaleTargetsに渡す
	- hitStopTimeを減らす
	- ヒットスローかヒットストップかを判定する
	- ヒットストップの場合は0.0を渡す
	- ヒットスローの場合、「中心のウェイト」「中心のタイムスケール」「中心の静止時間(ループ)」をそれぞれ考慮して、イン、ループ、アウトのいずれの状態かを計算したのち、Tweenでタイムスケール値を計算、各クラスに渡す

## Register(登録処理)
1. timeScaleTargetsに送られてきたITimeScaleTargetを追加する

## HitStop(ヒットストップ実行)
1. 引数として、かける時間
2. 引数をそれぞれ内部変数に格納する
	1. かける時間をhitStopTimeに入れる
	2. isHitSlowはfalse

## HitSlow(ヒットスロー実行)
1. 引数として、かける時間、中心のウェイト(0.0-1.0)、中心のタイムスケール、中心の静止時間、DoTweenのイージングフラグをもらう
2. 引数をそれぞれ内部変数に格納する
	1. 引数をそれぞれ適切な変数を用意して格納する
	2. isHitSlowはtrue

# タイムライン拡張
- タイムライン上で再生確認ができる実装を持つ(HitSlowPlayable/HitStopPlayable)
	- トラックを配置したら、エフェクト再生終了時間までの長さのトラックを自動生成する
	- トラックの長さは変更できず、エフェクトが調整されたらリアルタイムにトラックの長さを変更する

- タイムライン上の他のトラックに干渉し、プロパティを追加する
	- これはHitSlow/HitStopトラックが変更されるたび更新する
	- IHitStopTargetの対象となりうるオブジェクトに作用する。ルールは別記する。

## 他のトラックに影響を与えるもの
- Animator: Animator.speedを連動するプロパティアニメーションを追加し、タイムスケール値をフレームごとに計算して同期する。
- EffectPlayerTrack: PlayableBehaviourのspeedパラメータのアニメーションを追加し、タイムスケール値をフレームごとに計算して同期する。

## HitStopPlayable
**ファイル構成**:
- HitStopPlayableBehaviour.cs: PlayableBehaviourの実装
- HitStopPlayableAsset.cs: PlayableAssetの実装
- HitStopTrack.cs: TrackAssetの実装（赤色トラック）
- HitStopTrackMixer.cs: カスタムMixer実装（Editor専用）
- HitStopPlayableAssetEditor.cs: カスタムエディター

**機能**:
- Timeline上でヒットストップを再生
- トラックの長さは自動的にstopDurationに合わせて調整
- HitStopComposerシングルトンを使用（バインディング不要）
- **他のトラックに自動的に影響を与える**（Editor専用）

**パラメータ**:
- stopDuration: ヒットストップの時間（秒）

**他のトラックへの影響（HitStopTrackMixer実装）**:
- AnimationTrack: Animator.speedを0に設定（HitStop中）
- EffectPlayerTrack: ParticleSystem.simulationSpeedを0に設定（HitStop中）
- ProcessFrame毎にタイムスケール値（0 or 1）を計算して適用
- Graph停止時に自動的にspeed=1にリセット
- Editor専用実装（#if UNITY_EDITOR）

## HitSlowPlayable
**ファイル構成**:
- HitSlowPlayableBehaviour.cs: PlayableBehaviourの実装
- HitSlowPlayableAsset.cs: PlayableAssetの実装
- HitSlowTrack.cs: TrackAssetの実装（ピンク色トラック）
- HitSlowTrackMixer.cs: カスタムMixer実装（Editor専用）
- HitSlowPlayableAssetEditor.cs: カスタムエディター

**機能**:
- Timeline上でヒットスローを再生
- トラックの長さは自動的にslowDurationに合わせて調整
- HitStopComposerシングルトンを使用（バインディング不要）
- **他のトラックに自動的に影響を与える**（Editor専用）

**パラメータ**:
- slowDuration: ヒットスローの時間（秒）
- centerWeight: 中心のウェイト (0.0-1.0)
- centerTimeScale: 中心のタイムスケール (0.0-1.0)
- centerHoldTime: 中心の静止時間 (ループ)
- ease: DoTweenのイージングフラグ

**他のトラックへの影響（HitSlowTrackMixer実装）**:
- AnimationTrack: Animator.speedを制御（イン・ループ・アウト）
- EffectPlayerTrack: ParticleSystem.simulationSpeedを制御（イン・ループ・アウト）
- タイムスケール計算:
  - イン: 1.0 → centerTimeScale（イージング適用）
  - ループ: centerTimeScale（固定）
  - アウト: centerTimeScale → 1.0（イージング適用）
- DoTweenのイージングカーブを使用
- Graph停止時に自動的にspeed=1にリセット
- Editor専用実装（#if UNITY_EDITOR）