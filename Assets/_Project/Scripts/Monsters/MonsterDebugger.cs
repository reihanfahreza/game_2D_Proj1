using UnityEngine;

/// <summary>
/// Uji MonsterInstance tanpa battle (P4). Titik tiga component -> 1. Buat monster -> 3. Beri 100 EXP.
/// [ContextMenu]: method private bisa dipanggil dari menu Inspector.
/// </summary>
public class MonsterDebugger : MonoBehaviour
{
    [SerializeField] private MonsterData species;
    [SerializeField] private int level = 4;

    private MonsterInstance monster;

    [ContextMenu("1. Buat monster")]
    private void Create()
    {
        if (species == null) { Debug.LogWarning("Isi Species dulu."); return; }
        monster = new MonsterInstance(species, level);
        Log();
    }

    [ContextMenu("2. Serang 10 damage")]
    private void Hit()
    {
        if (monster == null) Create();
        if (monster == null) return;
        monster.TakeDamage(10);
        Log();
    }

    [ContextMenu("3. Beri 100 EXP")]
    private void GiveExp()
    {
        if (monster == null) Create();
        if (monster == null) return;
        foreach (GrowthEvent growth in monster.GainExp(100)) Debug.Log(growth.Message);
        Log();
    }

    private void Log() =>
        Debug.Log($"{monster.Data.DisplayName} Lv{monster.Level}  HP {monster.CurrentHP}/{monster.MaxHP}  " +
                  $"ATK {monster.Attack}  DEF {monster.Defense}  SPD {monster.Speed}  EXP {monster.Exp}/{monster.ExpToNext}");
}
