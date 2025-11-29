using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using Cysharp.Threading.Tasks;

/// <summary>
/// CueOrchestratorへ渡すコマンドを生成するためのツール
/// PlayableDirectorを継承
/// </summary>
[RequireComponent(typeof(PlayableDirector))]
public class CueTimelineDirector : MonoBehaviour
{
    [SerializeField]
    [Tooltip("BPM設定")]
    private float bpm = 120f;

    [SerializeField]
    [Tooltip("無視するトラック名のリスト")]
    private List<IgnoreTrackSetting> ignoreTrackSettings = new List<IgnoreTrackSetting>();

    private PlayableDirector _director;

    /// <summary>
    /// 無視するトラック設定
    /// </summary>
    [Serializable]
    public class IgnoreTrackSetting
    {
        [Tooltip("タイムライン名")]
        public string timelineName;

        [Tooltip("無視するトラック名リスト")]
        public List<string> trackNames = new List<string>();
    }

    /// <summary>
    /// BPM
    /// </summary>
    public float BPM
    {
        get => bpm;
        set => bpm = value;
    }

    /// <summary>
    /// PlayableDirector参照
    /// </summary>
    public PlayableDirector Director
    {
        get
        {
            if (_director == null)
            {
                _director = GetComponent<PlayableDirector>();
            }
            return _director;
        }
    }

    /// <summary>
    /// 無視するトラック設定
    /// </summary>
    public List<IgnoreTrackSetting> IgnoreTrackSettings => ignoreTrackSettings;

    /// <summary>
    /// 1拍の長さ（秒）
    /// </summary>
    public float BeatDuration => 60f / bpm;

    /// <summary>
    /// 指定ビートでの時間チェック
    /// </summary>
    /// <param name="time">チェックする時間</param>
    /// <param name="beatDivision">ビート分割数</param>
    /// <returns>ビートに一致しているか</returns>
    public bool IsOnBeat(float time, int beatDivision = 16)
    {
        float divisionDuration = BeatDuration * 4f / beatDivision;
        float remainder = time % divisionDuration;
        float tolerance = 0.001f;
        return remainder < tolerance || (divisionDuration - remainder) < tolerance;
    }

    /// <summary>
    /// トラックが無視対象かどうか
    /// </summary>
    /// <param name="timelineName">タイムライン名</param>
    /// <param name="trackName">トラック名</param>
    /// <returns>無視対象の場合true</returns>
    public bool IsIgnoredTrack(string timelineName, string trackName)
    {
        foreach (var setting in ignoreTrackSettings)
        {
            if (setting.timelineName == timelineName)
            {
                return setting.trackNames.Contains(trackName);
            }
        }
        return false;
    }

    private void Awake()
    {
        _director = GetComponent<PlayableDirector>();
    }
}
