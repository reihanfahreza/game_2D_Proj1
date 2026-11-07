using UnityEngine;

/// <summary>
/// Satu script satu tugas (P3): hanya membaca PlayerMovement lalu mengisi parameter Animator.
/// StringToHash: nama parameter diubah ke angka sekali saja.
/// Tantangan P3: animasi jalan 1,5x lebih cepat saat lari (parameter Speed sebagai Multiplier state Walk).
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int Speed = Animator.StringToHash("Speed");

    [SerializeField] private PlayerMovement movement;
    [SerializeField] private float runAnimationMultiplier = 1.5f;

    private Animator animator;

    void Awake() => animator = GetComponent<Animator>();

    void Update()
    {
        bool moving = movement.Velocity.sqrMagnitude > 0.01f;
        animator.SetBool(IsMoving, moving);
        animator.SetFloat(MoveX, movement.Facing.x);
        animator.SetFloat(MoveY, movement.Facing.y);
        animator.SetFloat(Speed, moving && movement.IsRunning ? runAnimationMultiplier : 1f);
    }
}
