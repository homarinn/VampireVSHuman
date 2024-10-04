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

    public int ReflectReceptionMillisecond = 1000;
    public int PerfectReflectReceptionMillisecond = 500;
    public int ReactionMillisecond = 3000;
    public int LastReactionMillisecond = 3000;

    private float shouldReflectTime = 0f;
    private float guardTime = 0f;

    public List<Sprite> DefaultSprites = new ();
    public List<Sprite> ReactionSprites = new ();
    public Sprite LastBurnedSprite;
    public List<Sprite> GuardSprites = new ();
    public List<Sprite> ReflectSprites = new ();
    private int BurnStatusIndex = 0;

    public CancellationTokenSource CancellationTokenSource = new();

    private bool canReflect = false;
    private bool canInput = false;

    public bool IsGuarding = false;

    private bool isActive = false;

    public Human Human;

    private void Awake()
    {
        renderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        InputHandler.Instance.OnLeftClickSubject.Where(_ => canInput).Subscribe(_ => Guard());
        Human.OpenCurtainTime.Skip(1).Subscribe(time => Blighted(time));
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
        if (!isActive)
        {
            return;
        }

        if (canReflect && !IsGuarding && IsClosedReception(Time.time - shouldReflectTime))
        {
            Burn();
            return;
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