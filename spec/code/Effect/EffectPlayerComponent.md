# EffectPlayerComponentクラス設計

## 実装
- MonoBehaviourを継承する
- EffectPlayerを持つ
- EffectPlayerをUnityのインスペクタ上から再生確認するための処理

# 概要
- エフェクトの再生確認と設定の簡易化のためのコンポーネント

# 外部設定変数(SerializeField)
- prefabKey: prefabDicのキー
- attachTarget: 再生時に対象オブジェクトの子供として生成する

# 処理
- EffectPlayerに処理や変数をリレーする

# タイムライン拡張
- タイムライン上でエフェクト再生を確認できる実装を持つ(EffectPlayerPlayable)
	- EffectPlayerComponentにバインドするトラック
	- BasicEffectを直接アサインする形式
	- トラックの長さは自動的にエフェクト再生時間に合わせて調整
	- クォンタイズ機能はTimeline上では使用しない（通常再生のみ）

## EffectPlayerPlayable実装（実装完了）
**ファイル構成**:
- EffectPlayerPlayableBehaviour.cs: PlayableBehaviourの実装
- EffectPlayerPlayableAsset.cs: PlayableAssetの実装
- EffectPlayerTrack.cs: TrackAssetの実装（オレンジ色トラック）
- EffectPlayerPlayableAssetEditor.cs: カスタムエディター

**機能**:
- Timeline上でBasicEffectを再生
- BasicEffectを直接アサインする形式（PrefabDictionaryのキーではない）
- EffectPlayerComponentにバインド
- **Editor専用** - BasicEffectのすべてのParticleSystemをタイムラインと同期して再生

**パラメータ**:
- attachTarget: エフェクトをアタッチする対象オブジェクト（nullの場合はアタッチしない）

**実装詳細（EffectPlayerPlayableBehaviour）**:
- `#if UNITY_EDITOR`で囲まれたEditor専用実装
- トラックのバインドからBasicEffectを取得（GameObjectにバインド）
  - ProcessFrame内でplayerDataからGameObjectを取得
  - GetComponent<BasicEffect>()でBasicEffectを取得
- OnBehaviourPlay: BasicEffectからすべてのParticleSystemを取得（GetComponentsInChildren）
- ProcessFrame: タイムライン時間に基づいてParticleSystem.Simulate()で同期再生
  - デルタタイムを計算して各ParticleSystemをSimulate
  - タイムライン巻き戻しに対応（負のデルタタイム検出時に再初期化）
- OnBehaviourPause/OnGraphStop: ParticleSystemをクリーンアップ
- 実際のシーンでは動作しない（Editorプレビュー専用）
- Timeline再生中、エフェクトがタイムラインと完全に同期

**トラックバインディング**:
- EffectPlayerTrack: GameObjectにバインド（BasicEffectを持つGameObject）
- バインドされたGameObjectからBasicEffectを自動取得
- CueSequence生成時、バインドからBasicEffectを優先的に取得（asset.basicEffectはフォールバック）

**attachTarget機能**:
- Timeline上でattachTargetを設定可能
- CueSequence生成時にattachTarget情報が含まれる
- Setup関数で`attachTarget{PrefabName}`としてGameObjectを受け取る
- 再生時に`EffectPlayer.SetAttachTarget()`を呼び出し、エフェクトをattachTargetの子供として生成