using System;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "InputSO", menuName = "Game/Input SO")]
public class InputSO : ScriptableObject, NewInput.IPlayerActions
{
    public event Action<Vector2> OnMove;
    public event Action<Vector2> OnLook;
    public event Action          OnJump;
    public event Action          OnAttack;
    public event Action          OnDashAttackPressed;
    public event Action          OnDashAttackReleased;
    public event Action          OnCrouch;
    public event Action          OnSlowTimeStarted;
    public event Action          OnSlowTimeStopped;
    public event Action          OnRestart;
    public event Action          OnStop;

    NewInput _input;

    void OnEnable()
    {
        _input = new NewInput();
        _input.Player.AddCallbacks(this);
        _input.Player.Enable();
    }

    void OnDisable()
    {
        _input.Player.RemoveCallbacks(this);
        _input.Player.Disable();
        if (Application.isPlaying)
            _input.Dispose();
    }

    void NewInput.IPlayerActions.OnMove(InputAction.CallbackContext ctx)
    {
        OnMove?.Invoke(ctx.ReadValue<Vector2>());
    }

    void NewInput.IPlayerActions.OnLook(InputAction.CallbackContext ctx)
    {
        OnLook?.Invoke(ctx.ReadValue<Vector2>());
    }

    void NewInput.IPlayerActions.OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) OnJump?.Invoke();
    }

    void NewInput.IPlayerActions.OnAttack(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) OnAttack?.Invoke();
    }

    void NewInput.IPlayerActions.OnDashAttack(InputAction.CallbackContext ctx)
    {
        if(ctx.started) OnDashAttackPressed?.Invoke();

        if (ctx.canceled) OnDashAttackReleased?.Invoke();
    }

    void NewInput.IPlayerActions.OnCrouch(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) OnCrouch?.Invoke();
    }

    void NewInput.IPlayerActions.OnSlowTime(InputAction.CallbackContext ctx)
    {
        if (ctx.started)   OnSlowTimeStarted?.Invoke();
        if (ctx.canceled)  OnSlowTimeStopped?.Invoke();
    }

    void NewInput.IPlayerActions.OnRestart(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) OnRestart?.Invoke();
    }

    void NewInput.IPlayerActions.OnStop(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) OnStop?.Invoke();
    }

    void NewInput.IPlayerActions.OnInteract(InputAction.CallbackContext ctx) { }
    void NewInput.IPlayerActions.OnPrevious(InputAction.CallbackContext ctx) { }
    void NewInput.IPlayerActions.OnNext(InputAction.CallbackContext ctx)     { }
}
