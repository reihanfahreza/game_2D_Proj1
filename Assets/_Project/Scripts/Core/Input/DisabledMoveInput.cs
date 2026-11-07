using UnityEngine;

/// <summary>Tugas P2: input gerak yang selalu nol. Pasang di GameBootstrap, Player diam tanpa mengubah PlayerMovement.</summary>
public class DisabledMoveInput : IMoveInput
{
    public Vector2 Move => Vector2.zero;
    public bool IsRunning => false;
}
