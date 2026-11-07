using UnityEngine;

/// <summary>
/// Pola decorator (P4): membungkus input asli. Saat terkunci, Move = nol dan Interact = false.
/// Dipakai battle, dialog (P6), menu jeda, dan perpindahan scene. Kunci memakai hitungan
/// supaya kunci bertumpuk (mis. dialog saat fade) tidak saling membuka.
/// </summary>
public class InputGate : IMoveInput, IActionInput
{
    private readonly IMoveInput move;
    private readonly IActionInput action;
    private int locks;

    public InputGate(IMoveInput moveInput, IActionInput actionInput)
    {
        move = moveInput;
        action = actionInput;
    }

    public bool IsLocked => locks > 0;

    public void Lock() => locks++;
    public void Unlock() => locks = Mathf.Max(0, locks - 1);
    public void ResetLocks() => locks = 0;

    public Vector2 Move => IsLocked ? Vector2.zero : move.Move;
    public bool IsRunning => !IsLocked && move.IsRunning;
    public bool InteractPressed => !IsLocked && action.InteractPressed;
}
