using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UniRx;
using System.Collections.Generic;
using System.Threading.Tasks;


#if UNITY_EDITOR
using UnityEditor;
#endif


public class Vampire : MonoBehaviour
{
    private SpriteRenderer renderer;

    public Human Human;

    public int ReflectReceptionMillisecond = 500;
    public int ReactionMillisecond = 3000;

    public List<Sprite> DefaultSprites = new ();
    public List<Sprite> ReactionSprites = new ();
    public Sprite LastBurnedSprite;
    public List<Sprite> ReflectSprites = new ();
    private int BurnStatusIndex = 0;

    public CancellationTokenSource CancellationTokenSource = new();

    private float elapsedTime = 0f;

    private bool canReflect = false;
    private bool canInput = false;

    private bool isActive = false;

    private void Awake()
    {
        renderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        InputHandler.Instance.OnLeftClickSubject.Skip(1).Where(_ => canInput).Subscribe(_ => Reflect());
        Human.OpenCurtainTime.Skip(1).Subscribe(time => EnableReflect());
        Human.CloseCurtainTime.Skip(1).Subscribe(time => Activate());
        Activate();
    }

    public void Activate()
    {
        EnableInput();
        // CancellationTokenSource = new();
        // LoopStart(CancellationTokenSource.Token);
        isActive = true;
        elapsedTime = 0f;
    }

    public void Deactivate()
    {
        DisableInput();
        DisableReflect();
        // CancellationTokenSource.Cancel();
        isActive = false;
    }

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        if (IsClosedReception(elapsedTime))
        {
            Deactivate();
            Burn();
            return;
        }

        if (canReflect)
        {
            elapsedTime += Time.deltaTime;
        }
    }

    // private async UniTaskVoid LoopStart(CancellationToken token)
    // {
    //     float elapsedTime = 0f;

    //     // Destroyされるまで無限ループ
    //     while (!token.IsCancellationRequested)
    //     {
    //         if (IsClosedReception(elapsedTime))
    //         {
    //             Deactivate();
    //             Burn();
    //             continue;
    //         }

    //         await UniTask.Yield();
    //         elapsedTime += Time.deltaTime;
    //     }

    //     DisableInput();
    // }

    private bool IsClosedReception(float elapsedTime)
    {
        return elapsedTime >= ((float) ReflectReceptionMillisecond) / 1000f;
    }

    private async void Blight()
    {
        renderer.sprite = ReflectSprites[BurnStatusIndex];
        Human.Blighted();

        await UniTask.Delay(ReactionMillisecond);

        ResetSprite();
        Human.CloseCurtain();
    }

    private async void Burn()
    {
        BurnStatusIndex++;

        if (BurnStatusIndex >= DefaultSprites.Count)
        {
            await BurnFinished();
        } else
        {
            renderer.sprite = ReactionSprites[BurnStatusIndex];
            Human.Smile();

            await UniTask.Delay(ReactionMillisecond);

            ResetSprite();
            Human.CloseCurtain();
        }
    }

    private void Reflect()
    {
        if (canReflect)
        {
            Deactivate();
            Blight();
        } else
        {
            Deactivate();
            FinishGame();
        }
    }

    private void ResetSprite()
    {
        renderer.sprite = DefaultSprites[BurnStatusIndex];
    }

    private async Task BurnFinished()
    {
        renderer.sprite = LastBurnedSprite;
        Human.Smile();
        Human.Deactivate();

        await UniTask.Delay(ReactionMillisecond);

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

    private void FinishGame()
    {
        // CancellationTokenSource.Cancel();
        // Human.CancellationTokenSource.Cancel();

        // ResetSprite();
        // Human.ResetSprite();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        // ビルドされたゲームではアプリケーションを終了する
        Application.Quit();
#endif
    }
}