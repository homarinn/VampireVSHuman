using UnityEngine;
using UnityEngine.InputSystem;
using UniRx;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class InputHandler : MonoBehaviour
{
    public static InputHandler Instance;

    private PlayerControls controls;

    public Subject<Unit> OnPushStartSubject = new ();
    public Subject<Unit> OnPushCancelSubject = new ();

    private void Awake()
    {
        Instance = this;
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.Enable();

        controls.Player.Push.started += OnPushStarted;
        controls.Player.Push.canceled += OnPushCanceled;
        controls.Player.Escape.performed += OnEscape;
    }

    private void OnDisable()
    {
        controls.Disable();

        controls.Player.Push.started -= OnPushStarted;
        controls.Player.Push.canceled -= OnPushCanceled;
        controls.Player.Escape.performed -= OnEscape;
    }

    private void OnPushStarted(InputAction.CallbackContext context)
    {
        OnPushStartSubject.OnNext(Unit.Default);
    }

    private void OnPushCanceled(InputAction.CallbackContext context)
    {
        OnPushCancelSubject.OnNext(Unit.Default);
    }

    private void OnEscape(InputAction.CallbackContext context)
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
    // ビルドされたゲームではアプリケーションを終了する
        Application.Quit();
#endif
    }
}
