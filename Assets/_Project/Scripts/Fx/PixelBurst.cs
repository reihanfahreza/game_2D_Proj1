using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Partikel piksel ringan dengan pool sendiri (P7). Struktur data: List aktif + Stack bebas,
/// jadi tidak ada Instantiate tiap percikan. Speed dalam piksel demo dibagi 16 (PPU), ukuran acak 1 atau 2 piksel.
/// Satu PixelBurst per scene (World &amp; Battle).
/// </summary>
public class PixelBurst : MonoBehaviour
{
    private class Particle
    {
        public SpriteRenderer sr;
        public Vector2 velocity;
        public float life;
        public float max;
    }

    [SerializeField] private Sprite pixel;          // UI_Pixel 4x4
    [SerializeField] private float gravity = 2.5f;
    [SerializeField] private string sortingLayer = "Characters";
    [SerializeField] private int sortingOrder = 100;

    private readonly List<Particle> active = new List<Particle>();
    private readonly Stack<Particle> free = new Stack<Particle>();

    public int ActiveCount => active.Count;

    public void Play(Vector3 pos, Color color, int count, float speed, float life)
    {
        for (int i = 0; i < count; i++)
        {
            Particle p = free.Count > 0 ? free.Pop() : Create();
            float a = Random.Range(0f, Mathf.PI * 2f);
            float s = Random.Range(speed * 0.3f, speed) / 16f;
            p.velocity = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s;
            p.life = p.max = life;
            p.sr.color = color;
            float size = Random.value < 0.5f ? 0.25f : 0.5f;    // 1 atau 2 piksel (sprite 4 px)
            p.sr.transform.localScale = new Vector3(size, size, 1f);
            p.sr.transform.position = pos;
            p.sr.gameObject.SetActive(true);
            active.Add(p);
        }
    }

    private Particle Create()
    {
        var go = new GameObject("Particle");
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = pixel;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = sortingOrder;
        go.SetActive(false);
        return new Particle { sr = sr };
    }

    void Update()
    {
        float dt = Time.deltaTime;
        for (int i = active.Count - 1; i >= 0; i--)   // mundur
        {
            Particle p = active[i];
            p.life -= dt;
            if (p.life <= 0f)
            {
                p.sr.gameObject.SetActive(false);
                active.RemoveAt(i);
                free.Push(p);                          // dipakai lagi
                continue;
            }
            p.velocity.y -= gravity * dt;
            p.sr.transform.position += (Vector3)(p.velocity * dt);
            Color c = p.sr.color;
            c.a = p.life / p.max;
            p.sr.color = c;
        }
    }
}
