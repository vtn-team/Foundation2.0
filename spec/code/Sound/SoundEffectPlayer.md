# SoundEffectPlayerクラス設計

## 実装
- MonoBehaviourを継承する
- AudioSourceを必須コンポーネントとする


# 概要
- ゲーム中のSE再生処理はこのクラスからアクセスする
- グローバルなサウンドの変数はSoundManagerが管理する


# 外部変数
- audioSource: サウンド再生機


# 外部インタフェース
- Play: SEを再生する。引数はない


# 処理フロー

## 初期化

## 再生
1. AudioSourceを使用して再生処理を実行する

# エディタ拡張
- SoundSourceDictionaryが生成したENUMのIDを設定するUIを用意する
	- 職値は0(INVALID)である
- 「サウンドを設定」ボタンを用意する
- 適切なENUMが設定されたうえで、「サウンドを設定」ボタンが押された場合、
  以下の処理を実行する
	1. AudioSourceをGetComponentする
		1. 存在しない場合はAudioSourceを作成する
	2. audioSource変数に取得したコンポーネントを設定する
	3. SoundSourceDictionaryからAudioClipを取得し、audioSourceのaudioClipにアサインする

# タイムライン拡張
- タイムライン上で再生確認ができる実装を持つ(SoundEffectPlayerPlayable)
	- トラックを配置したら、AudioClip再生終了時間までの長さのトラックを自動生成する
	- トラックの長さは変更できず、AudioClipが調整されたらリアルタイムにトラックの長さを変更する

## SoundEffectPlayerPlayable
**ファイル構成**:
- SoundEffectPlayerPlayableBehaviour.cs: PlayableBehaviourの実装
- SoundEffectPlayerPlayableAsset.cs: PlayableAssetの実装
- SoundEffectPlayerTrack.cs: TrackAssetの実装（青色トラック）
- SoundEffectPlayerPlayableAssetEditor.cs: カスタムエディター

**機能**:
- Timeline上でSEを再生
- AudioClipを直接アサインする形式
- トラックの長さは自動的にAudioClip.lengthに合わせて調整
- **バインド不要** - AudioSourceを直接生成して再生

**パラメータ**:
- audioClip: 再生するAudioClip（オブジェクト参照）
- estimatedDuration: 推定再生時間（秒）- AudioClipから自動取得

**エディタ機能**:
- AudioClipの変更時に自動的にestimatedDurationを更新
- "Auto Update Duration"ボタンでAudioClip.lengthから時間を取得

**実装詳細**:
- 共有AudioSourceを使用（static変数で管理）
- HideFlags.HideAndDontSaveでHierarchy非表示
- 参照カウント方式で複数Playable間でAudioSourceを共有
- OnGraphStart: 参照カウントをインクリメント
- OnGraphStop: 参照カウントをデクリメント、0になったらクリーンアップ
- Hierarchy汚染を完全に回避（Timeline_SharedAudioSource 1つのみ）