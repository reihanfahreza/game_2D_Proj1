using UnityEngine;

/// <summary>Suara battle (P6): hanya mendengarkan event BattleSystem.</summary>
public class BattleAudio : MonoBehaviour
{
    [SerializeField] private AudioClip hit;
    [SerializeField] private AudioClip hitCritical;
    [SerializeField] private AudioClip miss;
    [SerializeField] private AudioClip levelUp;
    [SerializeField] private AudioClip evolve;
    [SerializeField] private AudioClip victory;
    [SerializeField] private AudioClip faint;
    [SerializeField] private AudioClip heal;

    private BattleSystem battle;
    private AudioService audioService;

    public void Construct(BattleSystem battleSystem, AudioService audio)
    {
        battle = battleSystem;
        audioService = audio;
        if (audioService == null) return;

        battle.Hit += OnHit;
        battle.Missed += OnMissed;
        battle.LevelUp += OnLevelUp;
        battle.Evolving += OnEvolving;
        battle.Victory += OnVictory;
        battle.Fainted += OnFainted;
        battle.Healed += OnHealed;
    }

    private void OnHit(MonsterInstance target, int damage) => audioService.PlaySfx(battle.LastHitCritical ? hitCritical : hit);
    private void OnMissed() => audioService.PlaySfx(miss);
    private void OnLevelUp(MonsterData from, int level) => audioService.PlaySfx(levelUp, 0f);
    private void OnEvolving(MonsterData from, MonsterData to) => audioService.PlaySfx(evolve, 0f);
    private void OnVictory() => audioService.PlaySfx(victory, 0f);
    private void OnFainted(MonsterInstance m) => audioService.PlaySfx(faint, 0f);
    private void OnHealed(MonsterInstance m) => audioService.PlaySfx(heal, 0f);
}
