using UnityEngine;

/// <summary>
/// ゲーム中のSE再生処理を行うコンポーネント
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SoundEffectPlayer : MonoBehaviour
{
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip audioClip;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    /// <summary>
    /// SEを再生する
    /// </summary>
    public void Play()
    {
        if (audioSource == null || audioClip == null) return;

        audioSource.clip = audioClip;
        audioSource.Play();
    }

    /// <summary>
    /// 指定したAudioClipで再生する
    /// </summary>
    /// <param name="clip">再生するAudioClip</param>
    public void Play(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;

        audioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// 再生を停止する
    /// </summary>
    public void Stop()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    /// <summary>
    /// 再生中かどうか
    /// </summary>
    public bool IsPlaying => audioSource != null && audioSource.isPlaying;

    /// <summary>
    /// AudioClipを設定する
    /// </summary>
    /// <param name="clip">設定するAudioClip</param>
    public void SetAudioClip(AudioClip clip)
    {
        audioClip = clip;
        if (audioSource != null)
        {
            audioSource.clip = clip;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// エディタ用：AudioSourceを取得または作成
    /// </summary>
    public void SetupAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        audioSource.playOnAwake = false;
    }
#endif
}
