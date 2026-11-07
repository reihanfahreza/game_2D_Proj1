using UnityEngine;

/// <summary>
/// Data kecepatan (P2). 60 &amp; 96 piksel/detik dibagi PPU 16 = 3,75 dan 6 unit/detik.
/// Property => : class lain hanya bisa membaca.
/// </summary>
[CreateAssetMenu(fileName = "Cfg_Player", menuName = "Pulau Monster/Player Config")]
public class PlayerConfig : ScriptableObject
{
    [SerializeField] private float walkSpeed = 3.75f;
    [SerializeField] private float runSpeed = 6f;

    public float WalkSpeed => walkSpeed;
    public float RunSpeed => runSpeed;
}
