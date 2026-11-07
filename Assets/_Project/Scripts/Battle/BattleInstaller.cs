using UnityEngine;

/// <summary>Installer Scn_Battle (P4 placeholder -> P5 battle penuh -> P6 audio -> P7 BattleFeel).</summary>
public class BattleInstaller : SceneInstaller
{
    [SerializeField] private BattleSystem battle;
    [SerializeField] private BattleView view;
    [SerializeField] private SpriteRenderer background;
    [SerializeField] private Sprite forestBackground;
    [SerializeField] private Sprite caveBackground;
    [SerializeField] private BattleAudio battleAudio;
    [SerializeField] private BattleFeel feel;
    [SerializeField] private AudioClip battleMusic;
    [SerializeField] private AudioClip bossMusic;

    public BattleSystem Battle => battle;
    public BattleView View => view;

    public override void Install(GameServices s)
    {
        background.sprite = s.Battle.IsBoss ? caveBackground : forestBackground;
        battle.Construct(s.Party, s.Battle, s.Scenes);
        new BattlePresenter(battle, view);
        if (battleAudio != null) battleAudio.Construct(battle, s.Audio);
        if (feel != null) feel.Construct(battle);
        s.Audio?.PlayBattleMusic(s.Battle.IsBoss ? bossMusic : battleMusic);
        battle.Begin();
    }
}
