using UnityEngine;
using UnityEngine.Pool;

/// <summary>ObjectPool&lt;SpriteVfx&gt; bawaan Unity (P7) untuk efek tebasan.</summary>
public class VfxPool : MonoBehaviour
{
    [SerializeField] private SpriteVfx prefab;

    private ObjectPool<SpriteVfx> pool;

    void Awake()
    {
        pool = new ObjectPool<SpriteVfx>(
            createFunc: () =>
            {
                SpriteVfx vfx = Instantiate(prefab, transform);
                vfx.Init(this);
                return vfx;
            },
            actionOnGet: vfx => vfx.gameObject.SetActive(true),
            actionOnRelease: vfx => vfx.gameObject.SetActive(false),
            actionOnDestroy: vfx => Destroy(vfx.gameObject),
            collectionCheck: true,
            defaultCapacity: 4,
            maxSize: 16);
    }

    public void Play(Vector3 position)
    {
        SpriteVfx vfx = pool.Get();
        vfx.transform.position = position;
        vfx.Play();
    }

    public void Release(SpriteVfx vfx) => pool.Release(vfx);
}
