using UnityEngine.InputSystem;

/// <summary>Tombol Interact (E). WasPressedThisFrame bernilai true tepat satu frame saat tombol ditekan.</summary>
public class InputSystemActionInput : IActionInput
{
    private readonly InputAction interact;

    public InputSystemActionInput()
    {
        interact = InputSystem.actions.FindAction("Interact");
    }

    // Action Interact bawaan memakai interaction Hold: biarkan, cara ini tetap bereaksi saat tombol baru ditekan.
    public bool InteractPressed => interact != null && interact.WasPressedThisFrame();
}
