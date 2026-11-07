using UnityEngine;

/// <summary>
/// Gerak Player versi 5 (P1 v3 Rigidbody2D -> P2 v4 IMoveInput + Cfg_Player -> P3 v5 Facing).
/// Update membaca input tiap frame, FixedUpdate menggerakkan fisika pada interval tetap.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private PlayerConfig config;
    [SerializeField] private Rigidbody2D rb;

    private IMoveInput input;
    private Vector2 velocity;

    public Vector2 Velocity => velocity;
    public Vector2 Facing { get; private set; } = Vector2.down;
    public bool IsRunning { get; private set; }

    /// <summary>Pintu masuk dependency (dipanggil WorldInstaller).</summary>
    public void Construct(IMoveInput moveInput) => input = moveInput;

    void Update()
    {
        if (input == null) return;
        Vector2 move = input.Move;
        IsRunning = input.IsRunning;
        float speed = IsRunning ? config.RunSpeed : config.WalkSpeed;
        velocity = move * speed;
        if (move.sqrMagnitude > 0.01f) Facing = ToCardinal(move);
    }

    // linearVelocity sudah unit/detik: tanpa deltaTime
    void FixedUpdate() => rb.linearVelocity = velocity;

    /// <summary>Pindah langsung (load save, kalah battle). Velocity dinolkan agar tidak meluncur.</summary>
    public void Teleport(Vector2 position, Vector2 facing)
    {
        velocity = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        rb.position = position;
        transform.position = position;
        if (facing != Vector2.zero) Facing = ToCardinal(facing);
    }

    private static Vector2 ToCardinal(Vector2 v) =>
        Mathf.Abs(v.x) > Mathf.Abs(v.y)
            ? new Vector2(Mathf.Sign(v.x), 0f)
            : new Vector2(0f, Mathf.Sign(v.y));
}
