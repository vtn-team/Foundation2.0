using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 個別のサウンド再生を制御するクラス
/// MonoBehaviourを継承しないプレーンなC#クラス
/// </summary>
public class SoundPlayer
{
    /// <summary>
    /// プレイヤー識別用UUID
    /// </summary>
    public string PlayerId { get; private set; }

    /// <summary>
    /// 割り当てられたチャンネルID
    /// </summary>
    public int ChannelId { get; private set; } = -1;

    /// <summary>
    /// 再生中かどうか
    /// </summary>
    public bool IsPlaying => SoundManager.Instance.IsPlaying(PlayerId);

    /// <summary>
    /// SoundManagerとリンク済みかどうか
    /// </summary>
    public bool IsLinked => ChannelId >= 0;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public SoundPlayer()
    {
        PlayerId = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// サウンドを再生
    /// </summary>
    /// <param name="soundKey">再生キー</param>
    /// <param name="volume">音量（0.0〜1.0）</param>
    /// <param name="loopPlay">ループ再生</param>
    public void Play(string soundKey, float volume = 1.0f, bool loopPlay = false)
    {
        ChannelId = SoundManager.Instance.RequestPlay(PlayerId, soundKey, volume, loopPlay);
    }

    /// <summary>
    /// サウンドを再生し、完了まで待機
    /// </summary>
    /// <param name="soundKey">再生キー</param>
    /// <param name="volume">音量（0.0〜1.0）</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    public async UniTask PlayAsync(string soundKey, float volume = 1.0f, CancellationToken cancellationToken = default)
    {
        Play(soundKey, volume, false);

        if (ChannelId < 0)
        {
            return;
        }

        // 再生完了まで待機
        while (IsPlaying)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Stop();
                return;
            }

            await UniTask.Yield(cancellationToken);
        }
    }

    /// <summary>
    /// 再生を停止
    /// </summary>
    public void Stop()
    {
        SoundManager.Instance.RequestStop(PlayerId);
        ChannelId = -1;
    }

    /// <summary>
    /// 再生を一時停止
    /// </summary>
    public void Pause()
    {
        SoundManager.Instance.RequestPause(PlayerId);
    }

    /// <summary>
    /// 再生を再開
    /// </summary>
    public void Resume()
    {
        SoundManager.Instance.RequestResume(PlayerId);
    }

    /// <summary>
    /// 再生経過時間を取得
    /// </summary>
    /// <returns>経過時間（秒）</returns>
    public float GetPlaybackTime()
    {
        var info = SoundManager.Instance.GetPlaybackInfo(PlayerId);
        if (info != null && info.AudioSource != null)
        {
            return info.AudioSource.time;
        }
        return 0f;
    }

    /// <summary>
    /// 残り再生時間を取得
    /// </summary>
    /// <returns>残り時間（秒）</returns>
    public float GetRemainingTime()
    {
        var info = SoundManager.Instance.GetPlaybackInfo(PlayerId);
        if (info != null && info.AudioSource != null && info.AudioSource.clip != null)
        {
            return info.AudioSource.clip.length - info.AudioSource.time;
        }
        return 0f;
    }
}
