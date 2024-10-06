using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UniRx;

public class Human : MonoBehaviour
{
    private SpriteRenderer renderer;

    [Header("事前の挙動を何回するか")] public int PreambleCount = 2;
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

    private ReactiveProperty<float> openCurtainTime = new(0);
    public IReadOnlyReactiveProperty<float> OpenCurtainTime => openCurtainTime;

    private ReactiveProperty<float> closeCurtainTime = new(0);
    public IReadOnlyReactiveProperty<float> CloseCurtainTime => closeCurtainTime;

    private ReactiveProperty<float> faintCurtainTime = new(0);
    public IReadOnlyReactiveProperty<float> FaintCurtainTime => faintCurtainTime;

    [NonSerialized] public bool IsOpenCurtain = false;

    [Header("事前の挙動の画像登録")] public Sprite PreambleSprite;

    [Header("カーテンを開けた時の画像登録")] public SerializedDictionary<SpriteRenderer, Sprite> OpenCurtainSpriteMap = new();
    private Dictionary<SpriteRenderer, Sprite> defaultSpriteMap = new();

    [Header("フェイント時の画像登録")] public SerializedDictionary<SpriteRenderer, Sprite> FaintSpriteMap = new();

    [Header("反射された時の画像")] public Sprite BlightSprite;
    [Header("吸血鬼がガードに失敗した時の画像")] public Sprite SmileSprite;

    public CancellationTokenSource CancellationTokenSource = new();

    private bool isActive = false;

    public Vampire Vampire;

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
        preambleIntervalMillisecond = UnityEngine.Random.Range(MinPreambleIntervalMillisecond, MaxPreambleIntervalMillisecond+1);
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

        await UniTask.Delay(PreambleDurationMillisecond);

        ResetSprite();
    }

    public void Blighted()
    {
        renderer.sprite = BlightSprite;
    }

    public void Smile()
    {
        renderer.sprite = SmileSprite;
    }

    public void ResetSprite()
    {
        foreach (var defaultSpritePair in defaultSpriteMap)
        {
            defaultSpritePair.Key.sprite = defaultSpritePair.Value;
        }
    }
}
