using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static partial class DemoBuilder
{
    static GameObject pfbNpcBase;
    static readonly Dictionary<string, GameObject> npcPrefabs = new Dictionary<string, GameObject>();
    static GameObject pfbWild;
    static GameObject pfbSlash;

    static void CreatePrefabs()
    {
        EnsureFolder(PrefabDir + "/NPC");
        EnsureFolder(PrefabDir + "/Monsters");
        EnsureFolder(PrefabDir + "/Fx");

        // P3 Praktikum 6: rakit NPC (layer Interactable, Sorting Layer Characters, Box Collider 2D 0.8 x 0.5 di kaki)
        GameObject npc = NewObject("Pfb_Npc");
        npc.layer = LayerMask.NameToLayer("Interactable");
        SpriteRenderer npcBody = npc.AddComponent<SpriteRenderer>();
        npcBody.sprite = Sprites(ArtDir + "/Characters/NPC_Elder_Walk_16x16.png")[0];
        npcBody.sortingLayerName = "Characters";
        npcBody.spriteSortPoint = SpriteSortPoint.Pivot;
        var npcCollider = npc.AddComponent<BoxCollider2D>();
        npcCollider.size = new Vector2(0.8f, 0.5f);
        npcCollider.offset = new Vector2(0f, 0.25f);
        npc.AddComponent<Animator>();
        pfbNpcBase = PrefabUtility.SaveAsPrefabAsset(npc, PrefabDir + "/NPC/Pfb_Npc.prefab");
        Object.DestroyImmediate(npc);

        // Satu prefab, empat varian
        npcPrefabs.Clear();
        MakeNpcVariant<StarterGiver>("Elder", "Tetua Rahmat", giver =>
        {
            Set(giver, "starters", new Object[] { monsters["Pyrit"], monsters["Blubi"], monsters["Sprig"] });
            Set(giver, "starterLevel", 3);
            Set(giver, "getClip", Sfx("SFX_LevelUp"));
            SpriteRenderer marker = NewSprite("QuestMarker", giver.transform, new Vector3(0f, 1.35f, 0f),
                LoadSprite(ArtDir + "/UI/UI_Exclaim.png"), "Characters", 50);
            Set(giver, "questMarker", marker.gameObject);
        });
        MakeNpcVariant<SariNpc>("Girl", "Sari", sari => Set(sari, "dialogue", sariTips));
        MakeNpcVariant<GuardNpc>("Guard", "Penjaga Batas", guard => Set(guard, "asidePosition", new Vector2(22.5f, 10.4f)));
        MakeNpcVariant<MerchantNpc>("Merchant", "Pak Dagang", merchant =>
        {
            Set(merchant, "giftPotions", 3);
            Set(merchant, "giftClip", Sfx("SFX_Heal"));
        });

        // P4: Pfb_WildMonster (Rigidbody2D Dynamic, Freeze Rotation Z, layer Wild, tanda !)
        GameObject wild = NewObject("Pfb_WildMonster");
        wild.layer = LayerMask.NameToLayer("Wild");
        SpriteRenderer wildBody = wild.AddComponent<SpriteRenderer>();
        wildBody.sprite = monsters["Pyrit"].Frames[0];
        wildBody.sortingLayerName = "Characters";
        wildBody.spriteSortPoint = SpriteSortPoint.Pivot;
        var rb = wild.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.mass = 5f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var wildCollider = wild.AddComponent<BoxCollider2D>();
        wildCollider.size = new Vector2(0.7f, 0.4f);
        wildCollider.offset = new Vector2(0f, 0.2f);
        var wildAnimator = wild.AddComponent<Animator>();
        wildAnimator.runtimeAnimatorController = acMonster;
        SpriteRenderer alert = NewSprite("Alert", wild.transform, new Vector3(0f, 1.3f, 0f),
            LoadSprite(ArtDir + "/UI/UI_Alert.png"), "Characters", 60);
        alert.gameObject.SetActive(false);
        WildMonster wildMonster = wild.AddComponent<WildMonster>();
        Set(wildMonster, "species", monsters["Pyrit"]);
        Set(wildMonster, "body", wildBody);
        Set(wildMonster, "bodyCollider", wildCollider);
        Set(wildMonster, "rb", rb);
        Set(wildMonster, "animator", wildAnimator);
        Set(wildMonster, "alert", alert.gameObject);
        pfbWild = PrefabUtility.SaveAsPrefabAsset(wild, PrefabDir + "/Monsters/Pfb_WildMonster.prefab");
        Object.DestroyImmediate(wild);

        // P7: tebasan 4 frame untuk VfxPool
        Sprite[] slash = Sprites(ArtDir + "/VFX/VFX_HitSlash_4f_32x32.png");
        SpriteRenderer vfxRenderer = NewSprite("Pfb_Vfx_HitSlash", null, Vector3.zero, slash[0], "Characters", 90);
        vfxRenderer.transform.localScale = Vector3.one * 1.5f;
        SpriteVfx vfx = vfxRenderer.gameObject.AddComponent<SpriteVfx>();
        Set(vfx, "sr", vfxRenderer);
        Set(vfx, "frames", slash);
        Set(vfx, "frameTime", 0.05f);
        pfbSlash = PrefabUtility.SaveAsPrefabAsset(vfxRenderer.gameObject, PrefabDir + "/Fx/Pfb_Vfx_HitSlash.prefab");
        Object.DestroyImmediate(vfxRenderer.gameObject);
    }

    static void MakeNpcVariant<T>(string key, string npcName, Action<T> extra) where T : Npc
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(pfbNpcBase);
        instance.name = $"Pfb_Npc_{key}";
        Sprite[] sheet = Sprites($"{ArtDir}/Characters/NPC_{key}_Walk_16x16.png");
        var body = instance.GetComponent<SpriteRenderer>();
        body.sprite = sheet[0];
        var animator = instance.GetComponent<Animator>();
        animator.runtimeAnimatorController = npcAocs[key];

        T npc = instance.AddComponent<T>();
        Set(npc, "npcName", npcName);
        Set(npc, "body", body);
        Set(npc, "faceDown", sheet[0]);    // sprite hadap _0/_3/_6/_9
        Set(npc, "faceLeft", sheet[3]);
        Set(npc, "faceRight", sheet[6]);
        Set(npc, "faceUp", sheet[9]);
        Set(npc, "animator", animator);
        extra?.Invoke(npc);

        npcPrefabs[key] = PrefabUtility.SaveAsPrefabAsset(instance, $"{PrefabDir}/NPC/Pfb_Npc_{key}.prefab");
        Object.DestroyImmediate(instance);
    }

    static GameObject PlacePrefab(GameObject prefab, string name, Vector3 position, Transform parent = null)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = name;
        if (parent != null) instance.transform.SetParent(parent, false);
        instance.transform.position = position;
        return instance;
    }
}
