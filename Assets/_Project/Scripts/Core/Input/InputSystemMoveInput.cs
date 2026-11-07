using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Implementasi IMoveInput dengan Input System (keyboard &amp; gamepad). Class C# biasa, bukan MonoBehaviour.</summary>
public class InputSystemMoveInput : IMoveInput
{
    private readonly InputAction move;
    private readonly InputAction sprint;

    public InputSystemMoveInput()
    {
        // FindAction sekali di constructor, bukan tiap frame
        move = InputSystem.actions.FindAction("Move");
        sprint = InputSystem.actions.FindAction("Sprint");
    }

    public Vector2 Move => move != null ? move.ReadValue<Vector2>() : Vector2.zero;
    public bool IsRunning => sprint != null && sprint.IsPressed();
}
