using UnityEngine;

/// <summary>
/// Data statis satu spesies (P4), diisi dari MonsterDatabase.csv.
/// Field private + property read-only. Data hidup (level, HP) ada di MonsterInstance.
/// </summary>
[CreateAssetMenu(fileName = "Mon_", menuName = "Pulau Monster/Monster Data")]
public class MonsterData : ScriptableObject
{
    [SerializeField] private string id;               // "MON_001"
    [SerializeField] private string displayName;
    [SerializeField] private Element element;
    [SerializeField] private int stage = 1;
    [SerializeField] private Sprite[] frames;         // Idle0, Idle1, Attack, Hurt
    [SerializeField] private int baseHP, baseATK, baseDEF, baseSPD;
    [SerializeField] private string skillName = "Semburan Api";
    [SerializeField] private MonsterData evolvesTo;
    [SerializeField] private int evolveLevel;
    [SerializeField] private RuntimeAnimatorController idleController; // AOC_Mon_<Nama> (P4 Praktikum 3)

    public string Id => id;
    public string DisplayName => displayName;
    public Element Element => element;
    public int Stage => stage;
    public Sprite[] Frames => frames;
    public int BaseHP => baseHP;
    public int BaseATK => baseATK;
    public int BaseDEF => baseDEF;
    public int BaseSPD => baseSPD;
    public string SkillName => skillName;
    public MonsterData EvolvesTo => evolvesTo;
    public int EvolveLevel => evolveLevel;
    public RuntimeAnimatorController IdleController => idleController;
}
