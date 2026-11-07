using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Daun beterbangan saat pemain berjalan di rumput tinggi (P7): 3 daun tiap 0,15 s.</summary>
public class GrassRustle : MonoBehaviour
{
    [SerializeField] private Tilemap tallGrass;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PixelBurst burst;
    [SerializeField] private float interval = 0.15f;
    [SerializeField] private Color leaf = new Color32(124, 192, 97, 255);

    private float timer;

    void Update()
    {
        if (tallGrass == null || burst == null) return;
        if (movement.Velocity.sqrMagnitude < 0.01f) return;
        Vector3Int cell = tallGrass.WorldToCell(transform.position + Vector3.up * 0.1f);
        if (!tallGrass.HasTile(cell)) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = interval;
        burst.Play(transform.position + Vector3.up * 0.2f, leaf, 3, 25f, 0.35f);
    }
}
