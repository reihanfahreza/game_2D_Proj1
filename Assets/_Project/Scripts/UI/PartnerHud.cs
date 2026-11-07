using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD dunia versi demo (P5): panel partner (potret, nama, Lv, bar HP + angka, bar EXP)
/// dan panel area (nama area + Ramuan xN dari event PlayerParty.PotionsChanged).
/// </summary>
public class PartnerHud : MonoBehaviour
{
    [SerializeField] private GameObject partnerPanel;
    [SerializeField] private Image portrait;
    [SerializeField] private Text nameText;
    [SerializeField] private Text levelText;
    [SerializeField] private Text hpText;
    [SerializeField] private Image hpFill;
    [SerializeField] private Image expFill;
    [SerializeField] private Text areaText;
    [SerializeField] private Text potionsText;

    private PlayerParty party;
    private MonsterInstance watched;

    public void Construct(PlayerParty playerParty)
    {
        party = playerParty;
        party.PartnerChanged += OnPartnerChanged;
        party.PotionsChanged += Refresh;
        OnPartnerChanged();
    }

    void OnDestroy()
    {
        if (party != null)
        {
            party.PartnerChanged -= OnPartnerChanged;
            party.PotionsChanged -= Refresh;
        }
        if (watched != null) watched.Changed -= Refresh;
    }

    public void ShowArea(string areaName) => areaText.text = areaName;

    private void OnPartnerChanged()
    {
        if (watched != null) watched.Changed -= Refresh;
        watched = party.Partner;
        if (watched != null) watched.Changed += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        potionsText.text = $"Ramuan ×{party.Potions}";
        MonsterInstance m = party.Partner;
        partnerPanel.SetActive(m != null);
        if (m == null) return;

        nameText.text = m.Data.DisplayName;
        levelText.text = $"Lv{m.Level}";
        float fraction = m.MaxHP > 0 ? (float)m.CurrentHP / m.MaxHP : 0f;
        hpFill.fillAmount = fraction;
        hpFill.color = HpColors.For(fraction);
        hpText.text = $"{m.CurrentHP}/{m.MaxHP}";
        expFill.fillAmount = Mathf.Clamp01((float)m.Exp / m.ExpToNext);
    }

    void Update()
    {
        if (watched != null)
            portrait.sprite = watched.Data.Frames[Mathf.FloorToInt(Time.time / 0.35f) % 2];
    }
}
