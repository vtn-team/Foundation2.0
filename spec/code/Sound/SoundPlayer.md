# SoundPlayer

## 概要
個別のサウンド再生を制御するクラス。
プレイヤー識別用のUUIDとChannelIDを内部パラメータとして持ち、SoundManagerとリンクして再生状態を確認できる。
MonoBehaviourを継承しないプレーンなC#クラスとして設計され、任意のクラスからインスタンス化して使用する。

## 責務
- プレイヤー識別子（UUID）の生成と管理
- SoundManagerへの再生リクエスト送信
- 割り当てられたチャンネルIDの保持
- 再生状態の問い合わせ
- 再生停止・一時停止・再開の制御

## 使用例

### 基本的な使用方法
```csharp
public class PlayerController : MonoBehaviour
{
    // SoundPlayerをフィールドとして保持
    private SoundPlayer jumpSoundPlayer = new SoundPlayer();
    private SoundPlayer footstepSoundPlayer = new SoundPlayer();

    private void Jump()
    {
        // ジャンプ音を再生
        jumpSoundPlayer.Play("jump_se");
    }

    private void OnFootstep()
    {
        // 足音を再生
        footstepSoundPlayer.Play("footstep_se", volume: 0.8f);
    }

    private void OnDestroy()
    {
        // 明示的に停止（推奨）
        jumpSoundPlayer.Stop();
        footstepSoundPlayer.Stop();
    }
}
```

### 非同期再生（完了待機）
```csharp
public class CutsceneManager : MonoBehaviour
{
    private SoundPlayer voicePlayer = new SoundPlayer();

    public async UniTask PlayDialogue()
    {
        // ボイス再生が完了するまで待機（CancellationToken付き）
        await voicePlayer.PlayAsync("dialogue_001", 1.0f, this.GetCancellationTokenOnDestroy());

        // 次のボイスを再生
        await voicePlayer.PlayAsync("dialogue_002", 1.0f, this.GetCancellationTokenOnDestroy());

        Debug.Log("会話終了");
    }
}
```

### 再生状態の確認
```csharp
public class SoundStatusUI : MonoBehaviour
{
    private SoundPlayer bgmPlayer = new SoundPlayer();

    [SerializeField]
    private Text statusText;

    private void Update()
    {
        if (bgmPlayer.IsPlaying)
        {
            statusText.text = $"再生中: Channel {bgmPlayer.ChannelId}\n" +
                             $"経過: {bgmPlayer.GetPlaybackTime():F1}秒\n" +
                             $"残り: {bgmPlayer.GetRemainingTime():F1}秒";
        }
        else
        {
            statusText.text = "停止中";
        }
    }
}
```

### ループBGMの制御
```csharp
public class BGMController : MonoBehaviour
{
    private SoundPlayer bgmPlayer = new SoundPlayer();

    public void PlayBattleBGM()
    {
        bgmPlayer.Play("battle_bgm", volume: 1.0f, loopPlay: true);
    }

    public void PauseBGM()
    {
        bgmPlayer.Pause();
    }

    public void ResumeBGM()
    {
        bgmPlayer.Resume();
    }

    public void StopBGM()
    {
        bgmPlayer.Stop();
    }
}
```

### 複数のSoundPlayerの使い分け
```csharp
public class AudioManager : MonoBehaviour
{
    private SoundPlayer bgmPlayer = new SoundPlayer();
    private SoundPlayer sePlayer = new SoundPlayer();
    private SoundPlayer voicePlayer = new SoundPlayer();
    private SoundPlayer jinglePlayer = new SoundPlayer();

    public void PlayBGM(string key) => bgmPlayer.Play(key, loopPlay: true);
    public void PlaySE(string key) => sePlayer.Play(key);
    public void PlayVoice(string key) => voicePlayer.Play(key);
    public void PlayJingle(string key) => jinglePlayer.Play(key);

    private void OnDestroy()
    {
        bgmPlayer.Stop();
        sePlayer.Stop();
        voicePlayer.Stop();
        jinglePlayer.Stop();
    }
}
```

## 設計方針
- **UUID識別**: 各SoundPlayerは一意のUUIDで識別
- **疎結合**: SoundManagerとの通信はUUIDベース
- **状態追跡**: IsPlaying、ChannelIdでリアルタイム状態確認
- **非コンポーネント**: MonoBehaviour非継承で柔軟なインスタンス管理
- **非同期対応**: UniTaskでの再生完了待機をサポート
- **CancellationToken対応**: PlayAsyncでキャンセル可能

## 配置場所
- スクリプト: `unity/Assets/Scripts/System/Sound/SoundPlayer.cs`

## 依存関係
- UnityEngine
- UniTask
- SoundManager.cs

## 注意事項
- **手動停止**: MonoBehaviourではないため、OnDestroyで自動停止しない。明示的にStop()を呼ぶこと
- **UUID永続性**: インスタンスが維持される限りUUIDも維持される
- **単一再生**: 同一SoundPlayerで複数再生すると前の再生は停止される
- **チャンネル確認**: IsLinkedでSoundManagerとのリンク状態を確認可能
- **CancellationToken**: PlayAsyncではMonoBehaviour側からGetCancellationTokenOnDestroy()を渡すことを推奨
