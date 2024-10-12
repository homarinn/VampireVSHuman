using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    // シングルトンインスタンス
    public static SoundManager Instance { get; private set; }

    [Header("音源をここに登録")]
    public AudioClip MainBGM;
    public AudioClip OpenCurtainSE;
    public AudioClip CloseCurtainSE;
    public AudioClip FaintSE;
    public AudioClip SmileSE;
    public AudioClip EatSE;
    public AudioClip GuardSE;
    public AudioClip BurnSE;
    public AudioClip EnvironmentBGM;
    public AudioClip FlipSE;
    public AudioClip ResultSE;

    [Header("Audio Mixer")]
    public AudioMixer MainMixer;

    [Header("Audio Sources")]
    public AudioSource BGMSource;
    public AudioSource SFXSource;
    public AudioSource UISource;
    public AudioSource EnvironmentSource;

    private void Awake()
    {
        // シングルトンの設定
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // シーンをまたいで保持
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // AudioSourcesの初期設定
        SetupAudioSource(ref BGMSource, "BGM");
        SetupAudioSource(ref SFXSource, "SFX");
        SetupAudioSource(ref UISource, "UI");
        SetupAudioSource(ref EnvironmentSource, "Environment");
    }

    private void Start()
    {
        PlayBGM(MainBGM);
        PlayEnvironment(EnvironmentBGM);
    }

    /// <summary>
    /// AudioSourceを設定します。
    /// </summary>
    /// <param name="source">設定するAudioSource</param>
    /// <param name="groupName">AudioMixerグループ名</param>
    private void SetupAudioSource(ref AudioSource source, string groupName)
    {
        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
            var groups = MainMixer.FindMatchingGroups(groupName);
            if (groups.Length > 0)
            {
                source.outputAudioMixerGroup = groups[0];
            }
            else
            {
                Debug.LogError($"AudioMixer group '{groupName}' not found!");
            }

            if (groupName == "BGM" || groupName == "Environment")
                source.loop = true;
        }
    }

    #region BGM Methods

    /// <summary>
    /// BGMを再生します。
    /// </summary>
    /// <param name="clip">再生するAudioClip</param>
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null)
            return;

        if (BGMSource.clip == clip && BGMSource.isPlaying)
            return;

        BGMSource.clip = clip;
        BGMSource.Play();
    }

    /// <summary>
    /// BGMを再生します。
    /// </summary>
    /// <param name="clip">再生するAudioClip</param>
    /// <param name="source">鳴らすAudio Source</param>
    public void PlayBGM(AudioClip clip, AudioSource source)
    {
        if (clip == null)
            return;

        if (source.clip == clip && source.isPlaying)
            return;

        source.clip = clip;
        source.Play();
    }

    /// <summary>
    /// BGMを停止します。
    /// </summary>
    public void StopBGM()
    {
        BGMSource.Stop();
    }

    /// <summary>
    /// BGMを停止します。
    /// </summary>
    /// <param name="source">鳴らすAudio Source</param>
    public void StopBGM(AudioSource source)
    {
        source.Stop();
    }

    #endregion

    #region SFX Methods

    /// <summary>
    /// SFXを再生します。
    /// </summary>
    /// <param name="clip">再生するAudioClip</param>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null)
            return;

        SFXSource.PlayOneShot(clip);
    }

    /// <summary>
    /// SFXを再生します。
    /// </summary>
    /// <param name="clip">再生するAudioClip</param>
    /// <param name="source">鳴らすAudio Source</param>
    public void PlaySFX(AudioClip clip, AudioSource source)
    {
        if (clip == null)
            return;

        source.PlayOneShot(clip);
    }

    #endregion

    #region UI Methods

    /// <summary>
    /// UIサウンドを再生します。
    /// </summary>
    /// <param name="clip">再生するAudioClip</param>
    public void PlayUI(AudioClip clip)
    {
        if (clip == null)
            return;

        UISource.PlayOneShot(clip);
    }

    /// <summary>
    /// UIサウンドを再生します。
    /// </summary>
    /// <param name="clip">再生するAudioClip</param>
    /// <param name="source">鳴らすAudio Source</param>
    public void PlayUI(AudioClip clip, AudioSource source)
    {
        if (clip == null)
            return;

        source.PlayOneShot(clip);
    }

    /// <summary>
    /// Environmentを再生します。
    /// </summary>
    /// <param name="clip">再生するAudioClip</param>
    public void PlayEnvironment(AudioClip clip)
    {
        PlayBGM(clip, EnvironmentSource);
    }

    /// <summary>
    /// Environmentを再生します。
    /// </summary>
    /// <param name="clip">再生するAudioClip</param>
    /// <param name="source">鳴らすAudio Source</param>
    public void PlayEnvironment(AudioClip clip, AudioSource source)
    {
        PlayBGM(clip, source);
    }

    /// <summary>
    /// Environmentを停止します。
    /// </summary>
    public void StopEnvironment()
    {
        EnvironmentSource.Stop();
    }

    /// <summary>
    /// Environmentを停止します。
    /// </summary>
    /// <param name="source">鳴らすAudio Source</param>
    public void StopEnvironment(AudioSource source)
    {
        source.Stop();
    }

    #endregion

    #region Convenience Methods

    // 他の便利な再生メソッドをここに追加

    #endregion

    #region Volume Control

    /// <summary>
    /// BGMの音量を設定します。
    /// </summary>
    /// <param name="volume">音量（-80から0）</param>
    public void SetBGMVolume(float volume)
    {
        MainMixer.SetFloat("BGMVolume", volume);
    }

    /// <summary>
    /// SFXの音量を設定します。
    /// </summary>
    /// <param name="volume">音量（-80から0）</param>
    public void SetSFXVolume(float volume)
    {
        MainMixer.SetFloat("SFXVolume", volume);
    }

    /// <summary>
    /// UIの音量を設定します。
    /// </summary>
    /// <param name="volume">音量（-80から0）</param>
    public void SetUIVolume(float volume)
    {
        MainMixer.SetFloat("UIVolume", volume);
    }

    #endregion
}
