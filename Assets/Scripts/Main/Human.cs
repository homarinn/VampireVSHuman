using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UniRx;

public class Human : MonoBehaviour
{
    private SpriteRenderer renderer;

    [Header("事前挙動を何回するか")] public int PreambleCount = 2;
    private int currentPreambleCount = 0;

    [Header("事前挙動をするインターバルの最小時間")] public int MinPreambleIntervalMillisecond = 1000;
    [Header("事前挙動をするインターバルの最大時間")] public int MaxPreambleIntervalMillisecond = 3000;
    private int preambleIntervalMillisecond = 0;

    [Header("挙動画像を何ミリ秒表示するか")] public int PreambleDurationMillisecond = 500;

    private float lastPreambleTime = 0f;
    private float nextPreambleTime = 0f;

    [Header("カーテンを何ミリ秒開けるか(最小)")] public int MinOpenDurationMillisecond = 1000;
    [Header("カーテンを何ミリ秒開けるか(最大)")] public int MaxOpenDurationMillisecond = 3000;

    [Header("フェイント画像を表示する時間")] public int FaintMillisecond = 500;
    [Header("フェイントする確率")] public float FaintRate = 0.25f;

    [Header("大笑いするゲージの割合")] public float SmilePercentage = 70f;
    [NonSerialized] public bool IsSmiled = false;
    [Header("大笑いする時間")] public int SmileMillisecond = 2000;

    [Header("最初に事前挙動をするまでの時間")] public int FirstPreambleMillisecond = 2000;

    private ReactiveProperty<float> openCurtainTime = new(0);
    public IReadOnlyReactiveProperty<float> OpenCurtainTime => openCurtainTime;

    private ReactiveProperty<float> closeCurtainTime = new(0);
    public IReadOnlyReactiveProperty<float> CloseCurtainTime => closeCurtainTime;

    private ReactiveProperty<float> faintCurtainTime = new(0);
    public IReadOnlyReactiveProperty<float> FaintCurtainTime => faintCurtainTime;

    [NonSerialized] public bool IsOpenCurtain = false;

    [Header("事前挙動の画像")] public Sprite PreambleSprite;

    [Header("カーテンを開けた時の画像")] public SerializedDictionary<SpriteRenderer, Sprite> OpenCurtainSpriteMap = new();
    private Dictionary<SpriteRenderer, Sprite> defaultSpriteMap = new();

    [Header("フェイント時の画像")] public SerializedDictionary<SpriteRenderer, Sprite> FaintSpriteMap = new();

    [Header("大笑いの画像")] public Sprite SmileSprite;

    public CancellationTokenSource CancellationTokenSource = new();

    private bool isActive = false;

    public Vampire Vampire;
    public AudioSource AudioSource;

    private void Awake()
    {
        renderer = GetComponent<SpriteRenderer>();

        foreach (var openCurtainSpritePair in OpenCurtainSpriteMap)
        {
            defaultSpriteMap[openCurtainSpritePair.Key] = openCurtainSpritePair.Key.sprite;
        }

        foreach (var faintSpritePair in FaintSpriteMap)
        {
            defaultSpriteMap[faintSpritePair.Key] = faintSpritePair.Key.sprite;
        }
    }

    private void Start()
    {
        preambleIntervalMillisecond = FirstPreambleMillisecond;
        nextPreambleTime = GetNextPreambleTime();
        Activate();
    }

    public void Activate()
    {
        isActive = true;
    }

    public void Update()
    {
        if (!isActive)
        {
            return;
        }

        if (Time.time >= nextPreambleTime)
        {
            if (currentPreambleCount == PreambleCount)
            {
                currentPreambleCount = 0;
                preambleIntervalMillisecond = UnityEngine.Random.Range(MinPreambleIntervalMillisecond, MaxPreambleIntervalMillisecond+1);

                if (UnityEngine.Random.value <= FaintRate)
                {
                    Faint();
                } else
                {
                    Deactivate();
                    OpenCurtain();
                }
            } else
            {
                Preamble();
            }
        }
    }

    public void Deactivate()
    {
        isActive = false;
    }

    public async void OpenCurtain()
    {
        IsOpenCurtain = true;

        foreach (var openCurtainSpritePair in OpenCurtainSpriteMap)
        {
            openCurtainSpritePair.Key.sprite = openCurtainSpritePair.Value;
        }

        Deactivate();

        SoundManager.Instance.PlaySFX(SoundManager.Instance.OpenCurtainSE, AudioSource);

        openCurtainTime.Value = Time.time;

        int openDuration = UnityEngine.Random.Range(MinOpenDurationMillisecond, MaxOpenDurationMillisecond+1);

        await UniTask.Delay(openDuration);

        if (!isActive && Vampire.IsGuarding)
        {
            CloseCurtain();
        }
    }

    public void CloseCurtain()
    {
        IsOpenCurtain = false;

        SoundManager.Instance.PlaySFX(SoundManager.Instance.CloseCurtainSE, AudioSource);

        closeCurtainTime.Value = Time.time;
        nextPreambleTime = GetNextPreambleTime();

        ResetSprite();
        Activate();
    }

    public async void Faint()
    {
        foreach (var faintSpritePair in FaintSpriteMap)
        {
            faintSpritePair.Key.sprite = faintSpritePair.Value;
        }

        Deactivate();

        SoundManager.Instance.PlaySFX(SoundManager.Instance.FaintSE, AudioSource);

        await UniTask.Delay(FaintMillisecond);

        faintCurtainTime.Value = Time.time;
        nextPreambleTime = GetNextPreambleTime();

        ResetSprite();
        Activate();
    }

    public float GetNextPreambleTime()
    {
        float referenceTime = Mathf.Max(closeCurtainTime.Value, faintCurtainTime.Value, lastPreambleTime);
        return referenceTime + (preambleIntervalMillisecond / 1000f);
    }

    public async void Preamble()
    {
        renderer.sprite = PreambleSprite;
        currentPreambleCount++;

        lastPreambleTime = Time.time;
        nextPreambleTime = GetNextPreambleTime();

        SoundManager.Instance.PlaySFX(SoundManager.Instance.PreambleSE, AudioSource);

        await UniTask.Delay(PreambleDurationMillisecond);

        ResetSprite();
    }

    public async void Smile()
    {
        Deactivate();
        currentPreambleCount = 0;

        IsSmiled = true;
        renderer.sprite = SmileSprite;

        SoundManager.Instance.PlaySFX(SoundManager.Instance.SmileSE, AudioSource);

        await UniTask.Delay(SmileMillisecond);

        lastPreambleTime = Time.time;
        nextPreambleTime = GetNextPreambleTime();

        ResetSprite();
        Activate();
    }

    public void ResetSprite()
    {
        foreach (var defaultSpritePair in defaultSpriteMap)
        {
            defaultSpritePair.Key.sprite = defaultSpritePair.Value;
        }
    }
}
