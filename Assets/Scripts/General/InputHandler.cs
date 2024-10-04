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
    public Subject<Unit> OnLeftClickSubject = new ();

    private void Awake()
    {
        Instance = this;
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.Enable();

        controls.Player.LeftClick.started += OnLeftClickStarted;
        controls.Player.Escape.performed += OnEscape;
    }

    private void OnDisable()
    {
        controls.Disable();
        controls.Player.LeftClick.started -= OnLeftClickStarted;
        controls.Player.Escape.performed -= OnEscape;
    }

    private void OnLeftClickStarted(InputAction.CallbackContext context)
    {
        OnLeftClickSubject.OnNext(Unit.Default);
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
