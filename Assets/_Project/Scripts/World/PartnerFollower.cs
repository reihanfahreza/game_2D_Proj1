using UnityEngine;

/// <summary>
/// Partner berjalan di samping pemain (P4). Objek Partner (SpriteRenderer, layer Characters) di Scn_World.
/// Offset piksel demo dibagi PPU 16; tahap 2-3 dikecilkan 0,75.
/// </summary>
public class PartnerFollower : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;

    private PlayerParty party;
    private PlayerMovement player;

    public void Construct(PlayerParty playerParty, PlayerMovement movement)
    {
        party = playerParty;
        player = movement;
    }

    void LateUpdate()
    {
        MonsterInstance m = party != null ? party.Partner : null;
        body.enabled = m != null && player != null;
        if (!body.enabled) return;

        Vector2 f = player.Facing;
        Vector2 offset = f.y < 0 ? new Vector2(-12, 2)
                       : f.x < 0 ? new Vector2(10, 4)
                       : f.x > 0 ? new Vector2(-10, 4)
                       : new Vector2(12, -2);
        transform.position = player.transform.position + (Vector3)(offset / 16f);

        int frame = Mathf.FloorToInt(Time.time / 0.25f) % 2;
        body.sprite = m.Data.Frames[frame];
        body.flipX = f.x < 0;
        float s = m.Data.Frames[0].rect.width <= 16f ? 1f : 0.75f;
        transform.localScale = new Vector3(s, s, 1f);
    }
}
