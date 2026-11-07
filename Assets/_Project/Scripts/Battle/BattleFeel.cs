using System.Collections;
using UnityEngine;

/// <summary>
/// Game feel tanpa mengubah logika (P7): BattleFeel mendengarkan Hit &amp; Healed.
/// Tebasan 4 frame (VfxPool), percikan warna elemen (PixelBurst), guncang 0,2 s (kritis 0,35 s), hit-stop 0,06 s.
/// </summary>
public class BattleFeel : MonoBehaviour
{
    [SerializeField] private Transform cameraRig;
    [SerializeField] private SpriteRenderer playerBody;
    [SerializeField] private SpriteRenderer enemyBody;
    [SerializeField] private VfxPool hitVfx;
    [SerializeField] private PixelBurst burst;
    [SerializeField] private float shakeAmplitude = 0.15f;

    private BattleSystem battle;
    private Vector3 cameraHome;

    public void Construct(BattleSystem battleSystem)
    {
        battle = battleSystem;
        cameraHome = cameraRig.localPosition;
        battle.Hit += OnHit;
        battle.Healed += OnHealed;
        battle.LevelUp += OnLevelUp;
    }

    // Tugas akhir P7 (polish tambahan): kilau emas saat naik level
    private void OnLevelUp(MonsterData shown, int level) =>
        burst.Play(playerBody.bounds.center + Vector3.up * 0.5f, UiTheme.Gold, 24, 60f, 0.9f);

    private void OnHit(MonsterInstance target, int damage)
    {
        SpriteRenderer body = target == battle.Player ? playerBody : enemyBody;
        hitVfx.Play(body.bounds.center);                     // VfxPool
        Color c = battle.LastHitElement.HasValue
            ? StarterGiver.ElementColor(battle.LastHitElement.Value)
            : Color.white;
        burst.Play(body.bounds.center, c, battle.LastHitElement.HasValue ? 22 : 12, 90f, 0.5f);
        StartCoroutine(Shake(battle.LastHitCritical ? 0.35f : 0.2f));
        StartCoroutine(HitStop());                           // timeScale 0,05 selama 0,06 s
    }

    private void OnHealed(MonsterInstance t) =>
        burst.Play(playerBody.bounds.center, HpColors.High, 16, 50f, 0.6f);

    private IEnumerator Shake(float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            float k = 1f - t / seconds;
            cameraRig.localPosition = cameraHome + (Vector3)(Random.insideUnitCircle * shakeAmplitude * k);
            yield return null;
        }
        cameraRig.localPosition = cameraHome;
    }

    private IEnumerator HitStop()
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(0.06f);
        Time.timeScale = 1f;
    }

    void OnDestroy()
    {
        if (battle != null)
        {
            battle.Hit -= OnHit;
            battle.Healed -= OnHealed;
            battle.LevelUp -= OnLevelUp;
        }
        Time.timeScale = 1f;   // "Game lambat setelah battle" -> timeScale = 1 di OnDestroy
    }
}
