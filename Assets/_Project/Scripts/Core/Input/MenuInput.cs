using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Tombol menu (P5), sama dengan demo:
/// E / Spasi / Enter / Z = pilih, Esc / X / Backspace = batal, M = suara.
/// ?. menjaga dari keyboard yang belum terhubung.
/// </summary>
public static class MenuInput
{
    static Keyboard K => Keyboard.current;
    static Gamepad G => Gamepad.current;

    public static bool Up => Pressed(K?.upArrowKey, K?.wKey) || (G != null && G.dpad.up.wasPressedThisFrame);
    public static bool Down => Pressed(K?.downArrowKey, K?.sKey) || (G != null && G.dpad.down.wasPressedThisFrame);
    public static bool Left => Pressed(K?.leftArrowKey, K?.aKey) || (G != null && G.dpad.left.wasPressedThisFrame);
    public static bool Right => Pressed(K?.rightArrowKey, K?.dKey) || (G != null && G.dpad.right.wasPressedThisFrame);

    public static bool Confirm => Pressed(K?.eKey, K?.spaceKey, K?.enterKey, K?.numpadEnterKey, K?.zKey)
                                  || (G != null && G.buttonSouth.wasPressedThisFrame);

    public static bool Cancel => Pressed(K?.escapeKey, K?.xKey, K?.backspaceKey)
                                 || (G != null && G.buttonEast.wasPressedThisFrame);

    public static bool Pause => Pressed(K?.escapeKey) || (G != null && G.startButton.wasPressedThisFrame);

    public static bool Mute => Pressed(K?.mKey);

    private static bool Pressed(params KeyControl[] keys)
    {
        foreach (KeyControl key in keys)
            if (key != null && key.wasPressedThisFrame) return true;
        return false;
    }
}
