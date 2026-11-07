using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Logika battle bergiliran (P5) sebagai coroutine + event. Tidak tahu apa pun tentang UI:
/// BattlePresenter/BattleView/BattleAudio/BattleFeel hanya mendengarkan event.
/// Pesan lanjut sendiri (700-1200 ms + 18 ms x huruf); E / Spasi / Enter mempercepat.
/// </summary>
public class BattleSystem : MonoBehaviour
{
    [SerializeField] private float secondsPerChar = 0.018f;
    [SerializeField] private float evolveSeconds = 2.4f;
    [SerializeField] private int playerSkillUses = 3;
    [SerializeField] private int enemySkillUses = 2;
    [SerializeField, Range(0f, 1f)] private float enemySkillChance = 0.35f;

    private PlayerParty party;
    private BattleSession session;
    private SceneFlow scenes;
    private BattleAction chosen;
    private bool awaitingChoice;
    private BattleResult result = BattleResult.None;
    private int enemySkillsUsed;

    public BattleAction AttackChoice { get; } = new AttackAction();
    public BattleAction SkillChoice { get; } = new SkillAction();
    public BattleAction PotionChoice { get; } = new PotionAction();
    public BattleAction RunChoice { get; } = new RunAction();

    public MonsterInstance Player => party.Partner;
    public MonsterInstance Enemy { get; private set; }
    public bool IsBoss { get; private set; }
    public PlayerParty Party => party;
    public int SkillsLeft { get; private set; }
    public float EvolveSeconds => evolveSeconds;
    public bool LastHitCritical { get; private set; }
    public Element? LastHitElement { get; private set; }
    public BattleResult Result => result;
    public bool AwaitingChoice => awaitingChoice;

    public event Action Started;
    public event Action<string> MessageShown;
    public event Action ChoiceRequested;
    public event Action<MonsterInstance> Attacked;
    public event Action<MonsterInstance, int> Hit;
    public event Action Missed;
    public event Action<MonsterInstance> Healed;
    public event Action<MonsterInstance> SuperEffective;
    public event Action<MonsterInstance> Fainted;
    public event Action Victory;
    public event Action<MonsterData, int> LevelUp;
    public event Action<MonsterData, MonsterData> Evolving;
    public event Action<MonsterInstance> Evolved;
    public event Action<BattleResult> Ended;

    public void Construct(PlayerParty playerParty, BattleSession battleSession, SceneFlow sceneFlow)
    {
        party = playerParty;
        session = battleSession;
        scenes = sceneFlow;
        Enemy = session.Enemy;
        IsBoss = session.IsBoss;
        SkillsLeft = playerSkillUses;
    }

    public void Begin() => StartCoroutine(Run());

    /// <summary>Dipanggil Presenter saat pemain memilih aksi.</summary>
    public void Choose(BattleAction action)
    {
        if (!awaitingChoice || action == null) return;
        awaitingChoice = false;
        chosen = action;
    }

    public float DamageScale(MonsterInstance user) => user == Enemy && !IsBoss ? 0.8f : 1f;

    public void RaiseAttacked(MonsterInstance user) => Attacked?.Invoke(user);
    public void RaiseMissed() => Missed?.Invoke();
    public void RaiseHealed(MonsterInstance target) => Healed?.Invoke(target);
    public void RaiseSuperEffective(MonsterInstance user) => SuperEffective?.Invoke(user);

    public void RaiseHit(MonsterInstance target, int damage, bool critical, Element? element)
    {
        LastHitCritical = critical;
        LastHitElement = element;
        Hit?.Invoke(target, damage);
    }

    public void Escape() => result = BattleResult.Run;

    // Pesan tampil, lalu lanjut setelah (hold + panjang teks x 18 ms).
    // E / Spasi / Enter mempercepat setelah teks selesai diketik.
    public IEnumerator Say(string text, float hold = 0.8f)
    {
        MessageShown?.Invoke(text);       // View mengetik 55 huruf/detik
        float typed = text.Length * secondsPerChar;
        float wait = hold + typed;
        for (float t = 0f; t < wait; t += Time.deltaTime)
        {
            if (t > typed && MenuInput.Confirm) break;
            yield return null;
        }
    }

    private IEnumerator Run()
    {
        Started?.Invoke();
        yield return new WaitForSeconds(0.5f);                       // tirai pembuka
        yield return Say(IsBoss ? $"{Enemy.Data.DisplayName}, penguasa Gua Petir, muncul!"
                                : $"{Enemy.Data.DisplayName} liar Lv{Enemy.Level} muncul!", 0.9f);
        yield return Say($"Maju, {Player.Data.DisplayName}!", 0.7f);

        while (result == BattleResult.None)
        {
            chosen = null;
            awaitingChoice = true;
            ChoiceRequested?.Invoke();
            yield return new WaitUntil(() => chosen != null);

            if (chosen is RunAction && IsBoss)                        // boss: giliran tidak hilang
            {
                yield return chosen.Execute(this, Player, Enemy);
                continue;
            }

            BattleAction enemyAction = PickEnemyAction();             // 35% Jurus, maks 2 kali
            bool playerFirst = chosen.Priority > 0 || Player.Speed > Enemy.Speed
                               || (Player.Speed == Enemy.Speed && UnityEngine.Random.value < 0.5f);
            if (playerFirst)
            {
                yield return Turn(chosen, Player, Enemy);
                yield return Turn(enemyAction, Enemy, Player);
            }
            else
            {
                yield return Turn(enemyAction, Enemy, Player);
                yield return Turn(chosen, Player, Enemy);
            }
        }

        Ended?.Invoke(result);
        yield return new WaitForSeconds(0.3f);                        // jeda sebelum fade
        if (scenes != null) scenes.EndBattle(result);
    }

    private IEnumerator Turn(BattleAction action, MonsterInstance user, MonsterInstance target)
    {
        if (result != BattleResult.None || user.IsFainted) yield break;

        if (action is SkillAction)
        {
            if (user == Player) SkillsLeft = Mathf.Max(0, SkillsLeft - 1);
            else enemySkillsUsed++;
        }

        yield return action.Execute(this, user, target);
        if (result != BattleResult.None) yield break;                 // berhasil kabur

        if (Enemy.IsFainted) yield return Win();
        else if (Player.IsFainted) yield return Lose();
    }

    private BattleAction PickEnemyAction()
    {
        if (enemySkillsUsed < enemySkillUses && UnityEngine.Random.value < enemySkillChance) return SkillChoice;
        return AttackChoice;
    }

    private IEnumerator Win()
    {
        result = BattleResult.Win;
        Fainted?.Invoke(Enemy);                                       // lawan tenggelam & memudar
        yield return new WaitForSeconds(0.6f);
        yield return Say(IsBoss ? $"{Enemy.Data.DisplayName} kalah!" : $"{Enemy.Data.DisplayName} liar kalah!", 0.8f);
        Victory?.Invoke();                                            // jingle menang

        int exp = (10 + Enemy.Level * 8) * (IsBoss ? 2 : 1);
        yield return Say($"{Player.Data.DisplayName} mendapat {exp} EXP.", 0.9f);

        // EXP diberikan per satu level. GainExp langsung mengubah Level/HP/Data, jadi kalau semua EXP
        // diberikan sekaligus, HUD sudah memakai nilai akhir (mis. Lv 7) sementara pesan masih di
        // "naik ke Lv 5" - level di HUD terlihat melompat lalu mundur (5 -> 7 -> 6 -> 7).
        int left = exp;
        while (left > 0)
        {
            int step = Mathf.Min(left, Mathf.Max(1, Player.ExpToNext - Player.Exp));
            left -= step;
            foreach (GrowthEvent growth in Player.GainExp(step))
            {
                if (growth.Type == GrowthType.LevelUp)
                {
                    LevelUp?.Invoke(growth.From, growth.Level);
                    yield return Say(growth.Message, 0.9f);
                    continue;
                }
                yield return Say($"Apa? {growth.From.DisplayName} mulai bersinar...", 0.9f);
                Evolving?.Invoke(growth.From, growth.To);             // animasi 2,4 detik
                yield return new WaitForSeconds(evolveSeconds);
                Evolved?.Invoke(Player);                              // kilat putih + sprite baru
                yield return Say(growth.Message, 1.2f);               // "Pyrit berevolusi menjadi Pyrago!"
            }
        }

        if (!IsBoss)
        {
            int healed = Player.Heal(Mathf.RoundToInt(Player.MaxHP * 0.25f));
            if (healed > 0)
                yield return Say($"{Player.Data.DisplayName} beristirahat sejenak dan pulih {healed} HP.", 0.8f);
        }
    }

    private IEnumerator Lose()
    {
        result = BattleResult.Lose;
        Fainted?.Invoke(Player);
        yield return new WaitForSeconds(0.6f);
        yield return Say($"{Player.Data.DisplayName} pingsan!", 0.9f);
        yield return Say("Kamu bergegas kembali ke desa...", 0.9f);
    }
}
