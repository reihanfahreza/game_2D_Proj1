using System.Collections.Generic;

/// <summary>
/// MVP (P5): BattleSystem (logika) -> BattlePresenter -> BattleView/MonsterPanel (tampilan).
/// Class C# biasa; hidup selama BattleSystem memegang event-nya.
/// </summary>
public class BattlePresenter
{
    private readonly BattleSystem battle;
    private readonly BattleView view;
    private readonly List<BattleAction> actions = new List<BattleAction>();

    public BattlePresenter(BattleSystem battle, BattleView view)
    {
        this.battle = battle;
        this.view = view;

        battle.Started += OnStarted;                       // isi panel + PlayIntro
        battle.MessageShown += view.ShowMessage;
        battle.ChoiceRequested += OnChoiceRequested;       // label, usable, prompt, hint
        battle.Attacked += m => PanelOf(m).PlayLunge();
        battle.Hit += OnHit;                               // PlayHurt + angka damage
        battle.Fainted += m => PanelOf(m).PlayFaint();
        battle.LevelUp += (shown, level) => view.PlayerPanel.Show(battle.Player, false, shown, level);
        battle.Evolving += (from, to) =>
        {
            view.PlayerPanel.PlayEvolve(from, to, battle.EvolveSeconds);
            view.PlayEvolveRings(battle.EvolveSeconds);
        };
        battle.Evolved += m =>
        {
            view.PlayFlash();
            view.PlayerPanel.Show(m, false);
        };
        battle.SuperEffective += m =>
        {
            if (m == battle.Player) view.PlayerPanel.FlashStar();   // tugas P5: bintang saat sangat efektif
        };
        view.ActionClicked += OnActionClicked;
    }

    private MonsterPanel PanelOf(MonsterInstance m) => m == battle.Player ? view.PlayerPanel : view.EnemyPanel;

    private void OnStarted()
    {
        view.EnemyPanel.Show(battle.Enemy);
        view.PlayerPanel.Show(battle.Player);
        view.PlayIntro();
    }

    private void OnHit(MonsterInstance target, int damage)
    {
        MonsterPanel panel = PanelOf(target);
        panel.PlayHurt();
        view.ShowDamage(panel, damage, battle.LastHitCritical);
    }

    private void OnChoiceRequested()
    {
        actions.Clear();
        actions.Add(battle.AttackChoice);
        actions.Add(battle.SkillChoice);
        actions.Add(battle.PotionChoice);
        actions.Add(battle.RunChoice);

        MonsterInstance p = battle.Player;
        string[] labels = { "Serang", $"Jurus ({battle.SkillsLeft})", $"Ramuan ({battle.Party.Potions})", "Kabur" };
        bool[] usable = { true, battle.SkillsLeft > 0, battle.Party.Potions > 0 && p.CurrentHP < p.MaxHP, true };
        view.ShowMenu(labels, usable, $"Apa yang akan {p.Data.DisplayName} lakukan?", $"Jurus elemen: {p.Data.SkillName}");
    }

    private void OnActionClicked(int index)
    {
        if (index < 0 || index >= actions.Count) return;
        view.HideMenu();
        battle.Choose(actions[index]);
    }
}
