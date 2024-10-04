using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using System.Threading.Tasks;

public class Human : MonoBehaviour
{
    private SpriteRenderer renderer;

    public int MinOpenIntervalMillisecond = 1000;
    public int MaxOpenIntervalMillisecond = 5000;

    public int FaintMillisecond = 500;
    public float FaintRate = 0.25f;

    private ReactiveProperty<float> openCurtainTime = new(0);
    public IReadOnlyReactiveProperty<float> OpenCurtainTime => openCurtainTime;

    private ReactiveProperty<float> closeCurtainTime = new(0);
    public IReadOnlyReactiveProperty<float> CloseCurtainTime => closeCurtainTime;

    private ReactiveProperty<float> faintCurtainTime = new(0);
    public IReadOnlyReactiveProperty<float> FaintCurtainTime => faintCurtainTime;

    public SerializedDictionary<SpriteRenderer, Sprite> OpenCurtainSpriteMap = new();
    private Dictionary<SpriteRenderer, Sprite> defaultSpriteMap = new();

    public SerializedDictionary<SpriteRenderer, Sprite> FaintSpriteMap = new();

    public Sprite BlightSprite;
    public Sprite SmileSprite;

    private float nextOpenTime = 0f;

    private float elapsedTime = 0f;

    public CancellationTokenSource CancellationTokenSource = new();

    private bool isActive = false;

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
        nextOpenTime = GetRandomOpenTime();
        isActive = true;
    }

    public void Update()
    {
        if (!isActive)
        {
            return;
        }

        if (elapsedTime >= nextOpenTime)
        {
            if (UnityEngine.Random.value <= FaintRate)
            {
                Faint();
            } else
            {
                OpenCurtain();
            }
        }

        elapsedTime += Time.deltaTime;
    }

    public void Deactivate()
    {
        isActive = false;
    }

    public void OpenCurtain()
    {
        foreach (var openCurtainSpritePair in OpenCurtainSpriteMap)
        {
            openCurtainSpritePair.Key.sprite = openCurtainSpritePair.Value;
        }

        Deactivate();

        openCurtainTime.Value = elapsedTime;
    }

    public void CloseCurtain()
    {
        ResetSprite();

        closeCurtainTime.Value = elapsedTime;
        nextOpenTime = GetRandomOpenTime();

        Activate();
    }

    public async void Faint()
    {
        foreach (var faintSpritePair in FaintSpriteMap)
        {
            faintSpritePair.Key.sprite = faintSpritePair.Value;
        }

        Deactivate();

        faintCurtainTime.Value = elapsedTime;
        nextOpenTime = GetRandomOpenTime(); // 仮で

        await UniTask.Delay(FaintMillisecond);

        ResetSprite();
        nextOpenTime = GetRandomOpenTime();

        Activate();
    }

    public float GetRandomOpenTime()
    {
        float millisecond = UnityEngine.Random.Range(MinOpenIntervalMillisecond, MaxOpenIntervalMillisecond+1);
        float referenceTime = Mathf.Max(closeCurtainTime.Value, faintCurtainTime.Value);
        return referenceTime + (millisecond / 1000f);
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
