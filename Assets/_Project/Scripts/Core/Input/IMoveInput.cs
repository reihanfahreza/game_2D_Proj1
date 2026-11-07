using UnityEngine;

/// <summary>Kontrak input gerak (P2 - DI tahap 2). PlayerMovement hanya mengenal interface ini.</summary>
public interface IMoveInput
{
    Vector2 Move { get; }
    bool IsRunning { get; }
}
