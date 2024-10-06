using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UniRx;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Vampire : MonoBehaviour
{
    private SpriteRenderer renderer;

    [Header("何ミリ秒以内にガードすれば焼けないか")] public int ReflectReceptionMillisecond = 1000;
    [Header("何ミリ秒以内にガードすれば反射できるか")] public int PerfectReflectReceptionMillisecond = 500;
    [Header("焼けた時や反射した時の画像を何ミリ秒表示するか")] public int ReactionMillisecond = 3000;
    [Header("クリア時や3回失敗後の画像を何ミリ秒表示するか")] public int LastReactionMillisecond = 3000;

    private float shouldReflectTime = 0f;
    private float guardTime = 0f;

    [Header("何秒食べればクリアするか")] public float ClearSecond = 100;
    private float elapsedTime = 0f;
    private float allElapsedTime = 0f;

    [Header("食事中の画像")] public List<Sprite> DefaultSprites = new ();
    [Header("焼けてる時の画像")] public List<Sprite> ReactionSprites = new ();
    [Header("3回失敗後に表示する画像")] public Sprite LastBurnedSprite;
    [Header("ガード時に表示する画像")] public List<Sprite> GuardSprites = new ();
    [Header("反射時に表示する画像")] public List<Sprite> ReflectSprites = new ();
    private int BurnStatusIndex = 0;

    public CancellationTokenSource CancellationTokenSource = new();

    private bool canReflect = false;
    private bool canInput = false;

    [NonSerialized] public bool IsGuarding = false;

    private bool isActive = false;

    [Header("Perfectでクリア時間伸びるか")] public bool IsClearSecondIncrease = false;
    [Header("クリアゲージ自体が伸びるか")] public bool IsClearGageExpand = false;
    [Header("クリア時間の伸びる量")] public float IncreaseClearSecond = 3f;
    private float DefaultClearGageLength = 0f;
    private float InitialClearSecond = 0f;
    private RectTransform ClearGageRectTransform;

    public Human Human;
    public Slider ClearGageSlider;

    private void Awake()
    {
        renderer = GetComponent<SpriteRenderer>();
        ClearGageSlider.maxValue = ClearSecond;

        if (IsClearSecondIncrease)
        {
            ClearGageRectTransform = ClearGageSlider.GetComponent<RectTransform>();
            DefaultClearGageLength = ClearGageRectTransform.sizeDelta.y;
            InitialClearSecond = ClearSecond;
        }
    }

    private void Start()
    {
        InputHandler.Instance.OnLeftClickSubject.Where(_ => canInput).Subscribe(_ => Guard());
        Human.OpenCurtainTime.Skip(1).Subscribe(time => Blighted(time));
        Human.CloseCurtainTime.Skip(1).Subscribe(time => Activate());

        allElapsedTime = 0;
        Activate();
    }

    public void Activate()
    {
        if (!IsGuarding)
        {
            ResetSprite();
        }

        EnableInput();
        DisableReflect();
        isActive = true;
    }

    public void Deactivate()
    {
        DisableInput();
        DisableReflect();
        Human.Deactivate();
        IsGuarding = false;
        isActive = false;
    }

    private void Update()
    {
        allElapsedTime += Time.deltaTime;

        if (!isActive)
        {
            return;
        }

        if (canReflect && !IsGuarding && IsClosedReception(Time.time - shouldReflectTime))
        {
            Burn();
            return;
        }

        if (!IsGuarding)
        {
            elapsedTime += Time.deltaTime;
            ClearGageSlider.value = elapsedTime;

            if (elapsedTime >= ClearSecond)
            {
                ClearGageSlider.value = ClearSecond;

                // TODO: クリア処理
                Debug.Log(allElapsedTime);
                FinishGame();
            }
        }
    }

    private bool IsClosedReception(float elapsedTime)
    {
        return elapsedTime >= ((float) Mathf.Min(ReflectReceptionMillisecond, Human.MinOpenDurationMillisecond)) / 1000f;
    }

    private async void Burn()
    {
        BurnStatusIndex++;

        Deactivate();

        renderer.sprite = ReactionSprites[BurnStatusIndex - 1];
        Human.Smile();

        await UniTask.Delay(ReactionMillisecond);

        if (BurnStatusIndex >= DefaultSprites.Count)
        {
            await BurnFinished();
        } else
        {
            ResetSprite();
            Human.CloseCurtain();
        }
    }

    private void Blighted(float time)
    {
        EnableReflect();
        shouldReflectTime = time;

        if (IsGuarding && IsPerfect())
        {
            Blight();
        }
    }

    private void Guard()
    {
        ToggleGuard();

        if (IsGuarding)
        {
            guardTime = Time.time;

            if (canReflect && IsPerfect())
            {
                Blight();
            } else
            {
                renderer.sprite = GuardSprites[BurnStatusIndex];
            }
        } else
        {
            guardTime = 0f;
            renderer.sprite = DefaultSprites[BurnStatusIndex];
        }
    }

    private async void Blight()
    {
        Deactivate();

        renderer.sprite = ReflectSprites[BurnStatusIndex];
        Human.Blighted();

        await UniTask.Delay(ReactionMillisecond);

        if (IsClearSecondIncrease)
        {
            ClearSecond += IncreaseClearSecond;
            ClearGageSlider.maxValue = ClearSecond;
            if (IsClearGageExpand)
            {
                float afterLength = DefaultClearGageLength * ClearSecond / InitialClearSecond;
                Vector2 sizeDelta = ClearGageRectTransform.sizeDelta;
                sizeDelta.y = afterLength;
                ClearGageRectTransform.sizeDelta = sizeDelta;
            }
        } else
        {
            elapsedTime += ReactionMillisecond / 1000f;
        }

        ResetSprite();
        Human.CloseCurtain();
    }

    private void ResetSprite()
    {
        renderer.sprite = DefaultSprites[BurnStatusIndex];
    }

    private async Task BurnFinished()
    {
        renderer.sprite = LastBurnedSprite;

        await UniTask.Delay(LastReactionMillisecond);

        FinishGame();
    }

    private void EnableInput()
    {
        canInput = true;
    }

    private void DisableInput()
    {
        canInput = false;
    }

    private void EnableReflect()
    {
        canReflect = true;
    }

    private void DisableReflect()
    {
        canReflect = false;
    }

    private void ToggleGuard()
    {
        IsGuarding = !IsGuarding;
    }

    private bool IsPerfect()
    {
        return Mathf.Abs(guardTime - shouldReflectTime) * 1000f <= PerfectReflectReceptionMillisecond;
    }

    private void FinishGame()
    {
        Deactivate();

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        // ビルドされたゲームではアプリケーションを終了する
        Application.Quit();
#endif
    }
}