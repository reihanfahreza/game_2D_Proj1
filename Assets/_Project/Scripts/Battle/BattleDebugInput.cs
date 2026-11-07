using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Alat uji battle (hanya Editor / Development Build):
/// F1 = HP lawan tinggal 1 (serangan berikutnya menang), F2 = HP partner tinggal 1.
/// </summary>
public class BattleDebugInput : MonoBehaviour
{
    [SerializeField] private BattleSystem battle;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void Update()
    {
        Keyboard k = Keyboard.current;
        if (k == null || battle == null || battle.Enemy == null || battle.Player == null) return;
        if (k.f1Key.wasPressedThisFrame && battle.Enemy.CurrentHP > 1) battle.Enemy.TakeDamage(battle.Enemy.CurrentHP - 1);
        if (k.f2Key.wasPressedThisFrame && battle.Player.CurrentHP > 1) battle.Player.TakeDamage(battle.Player.CurrentHP - 1);
    }
#endif
}
