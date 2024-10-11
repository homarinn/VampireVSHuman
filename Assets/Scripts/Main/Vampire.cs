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

    [Header("何秒食べればクリアするか")] public float ClearSecond = 100;

    [Header("食事中の画像")] public Sprite DefaultSprite;
    [Header("ガード時に表示する画像")] public Sprite GuardSprite;

    [Header("焼けた画像")] public Sprite BurnedSprite;
    [Header("焼けた画像を何ミリ秒表示するか")] public int BurnedMillisecond = 2000;

    private bool canInput = false;

    [NonSerialized] public bool IsGuarding = false;

    [Header("何秒毎に食事SEを鳴らすか")] public float EatSEInterval = 3f;
    private float elapsedTimeFromLastGuard = 0f;

    private bool isActive = false;

    public Human Human;
    public Slider ClearGageSlider;
    public AudioSource AudioSource;

    private void Awake()
    {
        renderer = GetComponent<SpriteRenderer>();
        ClearGageSlider.maxValue = ClearSecond;
    }

    private void Start()
    {
        InputHandler.Instance.OnPushStartSubject.Where(_ => canInput).Subscribe(_ => StartGuard());
        InputHandler.Instance.OnPushCancelSubject.Where(_ => canInput).Subscribe(_ => CancelGuard());
        Human.OpenCurtainTime.Skip(1).Subscribe(time => Blighted());
        Human.CloseCurtainTime.Skip(1).Subscribe(time => Activate());

        Activate();
    }

    public void Activate()
    {
        if (!IsGuarding)
        {
            ResetSprite();
        }

        EnableInput();

        isActive = true;
    }

    public void Deactivate()
    {
        DisableInput();
        Human.Deactivate();
        IsGuarding = false;

        isActive = false;
    }

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        if (!IsGuarding)
        {
            if (Human.IsOpenCurtain)
            {
                Burn();
                return;
            } else
            {
                elapsedTimeFromLastGuard += Time.deltaTime;
                ClearGageSlider.value += Time.deltaTime;

                if (ClearGageSlider.value >= ClearSecond)
                {
                    ShowResult();
                    return;
                } else if (ClearGageSlider.value >= ClearSecond * Human.SmilePercentage/100 && !Human.IsSmiled)
                {
                    Human.Smile();
                }

                if (elapsedTimeFromLastGuard >= EatSEInterval)
                {
                    elapsedTimeFromLastGuard = 0f;
                    SoundManager.Instance.PlaySFX(SoundManager.Instance.EatSE, AudioSource);
                }
            }
        } else
        {
            elapsedTimeFromLastGuard = 0f;
        }
    }

    private async void Burn()
    {
        Deactivate();

        renderer.sprite = BurnedSprite;

        SoundManager.Instance.PlaySFX(SoundManager.Instance.BurnSE, AudioSource);

        await UniTask.Delay(BurnedMillisecond);

        FinishGame();
    }

    private void Blighted()
    {
        if (!IsGuarding)
        {
            // 即死
            Burn();
        }
    }

    private void StartGuard()
    {
        SoundManager.Instance.PlaySFX(SoundManager.Instance.GuardSE, AudioSource);
        IsGuarding = true;
        renderer.sprite = GuardSprite;
    }

    private void CancelGuard()
    {
        IsGuarding = false;
        ResetSprite();
    }

    private void ResetSprite()
    {
        renderer.sprite = DefaultSprite;
    }

    private void EnableInput()
    {
        canInput = true;
    }

    private void DisableInput()
    {
        canInput = false;
    }

    private void ShowResult()
    {
        Deactivate();
        FinishGame();
    }

    private void FinishGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        // ビルドされたゲームではアプリケーションを終了する
        Application.Quit();
#endif
    }
}