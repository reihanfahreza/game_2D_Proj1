using UnityEngine;

/// <summary>
/// Scene sandbox P1 (Scn_Reihan) tidak lewat Scn_Bootstrap, jadi Player diberi input langsung di sini.
/// </summary>
public class SandboxStarter : MonoBehaviour
{
    [SerializeField] private PlayerMovement player;

    void Start() => player.Construct(new InputSystemMoveInput());
}
