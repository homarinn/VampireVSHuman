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

    public int ReflectReceptionMillisecond = 1000;
    public int ReactionMillisecond = 3000;
    public int LastReactionMillisecond = 3000;

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
        InputHandler.Instance.OnLeftClickSubject.Where(_ => canInput).Subscribe(_ => Reflect());
        Human.OpenCurtainTime.Skip(1).Subscribe(time => EnableReflect());
        Human.CloseCurtainTime.Skip(1).Subscribe(time => Activate());
        Activate();
    }

    public void Activate()
    {
        EnableInput();
        isActive = true;
        elapsedTime = 0f;
    }

    public void Deactivate()
    {
        DisableInput();
        DisableReflect();
        Human.Deactivate();
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
            Burn();
            return;
        }

        if (canReflect)
        {
            elapsedTime += Time.deltaTime;
        }
    }

    private bool IsClosedReception(float elapsedTime)
    {
        return elapsedTime >= ((float) ReflectReceptionMillisecond) / 1000f;
    }

    private async void Blight()
    {
        Deactivate();

        renderer.sprite = ReflectSprites[BurnStatusIndex];
        Human.Blighted();

        await UniTask.Delay(ReactionMillisecond);

        ResetSprite();
        Human.CloseCurtain();
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

    private void Reflect()
    {
        if (canReflect)
        {
            Blight();
        } else
        {
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