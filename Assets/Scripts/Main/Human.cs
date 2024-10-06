using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using System.Threading.Tasks;

public class Human : MonoBehaviour
{
    private SpriteRenderer renderer;

    [Header("事前の挙動を何回するか")] public int PreambleCount = 2;
    private int currentPreambleCount = 0;
    [Header("挙動の間隔")] public int PreambleIntervalMillisecond = 1000;
    private float lastPreambleTime = 0f;
    [Header("挙動画像を何ミリ秒表示するか")] public int PreambleDurationMillisecond = 1000;
    private float nextPreambleTime = 0f;

    // [Header("カーテンを開けるインターバルの最小時間")] public int MinOpenIntervalMillisecond = 1000;
    // [Header("カーテンを開けるインターバルの最大時間")] public int MaxOpenIntervalMillisecond = 5000;

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

    [Header("事前の挙動の画像登録")] public Sprite PreambleSprite;

    [Header("カーテンを開けた時の画像登録")] public SerializedDictionary<SpriteRenderer, Sprite> OpenCurtainSpriteMap = new();
    private Dictionary<SpriteRenderer, Sprite> defaultSpriteMap = new();

    [Header("フェイント時の画像登録")] public SerializedDictionary<SpriteRenderer, Sprite> FaintSpriteMap = new();

    [Header("反射された時の画像")] public Sprite BlightSprite;
    [Header("吸血鬼がガードに失敗した時の画像")] public Sprite SmileSprite;

    // private float nextOpenTime = 0f;

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
        Activate();
    }

    public void Activate()
    {
        // nextOpenTime = GetRandomOpenTime();
        nextPreambleTime = GetNextPreambleTime();
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

                // if (UnityEngine.Random.value <= FaintRate)
                // {
                //     Faint();
                // } else
                // {
                //     Deactivate();
                //     OpenCurtain();
                // }

                if (UnityEngine.Random.value > FaintRate)
                {
                    Deactivate();
                    OpenCurtain();
                }
            } else
            {
                Preamble();
            }
        }

        // if (Time.time >= nextOpenTime)
        // {
        //     if (UnityEngine.Random.value <= FaintRate)
        //     {
        //         Faint();
        //     } else
        //     {
        //         Deactivate();
        //         OpenCurtain();
        //     }
        // }
    }

    public void Deactivate()
    {
        isActive = false;
    }

    public async void OpenCurtain()
    {
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
        ResetSprite();

        closeCurtainTime.Value = Time.time;
        // nextOpenTime = GetRandomOpenTime();
        nextPreambleTime = GetNextPreambleTime();

        Activate();
    }

    public async void Faint()
    {
        foreach (var faintSpritePair in FaintSpriteMap)
        {
            faintSpritePair.Key.sprite = faintSpritePair.Value;
        }

        Deactivate();

        // nextOpenTime = GetRandomOpenTime(); // 仮で

        await UniTask.Delay(FaintMillisecond);

        faintCurtainTime.Value = Time.time;

        ResetSprite();
        // nextOpenTime = GetRandomOpenTime();
        nextPreambleTime = GetNextPreambleTime();

        Activate();
    }

    // public float GetRandomOpenTime()
    // {
    //     float millisecond = UnityEngine.Random.Range(MinOpenIntervalMillisecond, MaxOpenIntervalMillisecond+1);
    //     float referenceTime = Mathf.Max(closeCurtainTime.Value, faintCurtainTime.Value);
    //     return referenceTime + (millisecond / 1000f);
    // }

    public float GetNextPreambleTime()
    {
        float referenceTime = Mathf.Max(closeCurtainTime.Value, faintCurtainTime.Value, lastPreambleTime);
        return referenceTime + (PreambleIntervalMillisecond / 1000f);
    }

    public async void Preamble()
    {
        renderer.sprite = PreambleSprite;
        currentPreambleCount++;
        Deactivate();

        await UniTask.Delay(PreambleDurationMillisecond);

        lastPreambleTime = Time.time;

        ResetSprite();
        nextPreambleTime = GetNextPreambleTime();

        Activate();
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
