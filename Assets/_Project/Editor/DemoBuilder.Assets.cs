using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Presets;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

public static partial class DemoBuilder
{
    static readonly Vector2 PivotBottom = new Vector2(0.5f, 0f);
    static readonly Vector2 PivotCenter = new Vector2(0.5f, 0.5f);

    // Indeks slice Tiles_Overworld_16x16 (baris x 12 + kolom), sesuai Tiles_Overworld_index.txt
    static readonly string[] TileNames =
    {
        "Path_TL", "Path_T", "Path_TR", "Path_iTL", "Path_iTR", "Water_TL", "Water_T", "Water_TR", "Water_iTL", "Water_iTR", "Grass_A", "Grass_B",
        "Path_L", "Path_C", "Path_R", "Path_iBL", "Path_iBR", "Water_L", "Water_C", "Water_R", "Water_iBL", "Water_iBR", "Grass_Flower", "Sand",
        "Path_BL", "Path_B", "Path_BR", "TallGrass_A", "TallGrass_B", "Water_BL", "Water_B", "Water_BR", "Flowers", "SmallRock", "Sign", "SaveStone",
        "Tree_Round", "Tree_Pine", "Bush", "BigRock", "Stump", "Fence_H", "Fence_V", "Cave_Floor_A", "Cave_Floor_B", "Cave_Wall", "Cave_Entrance", "Crystal",
        "Roof_L", "Roof_M", "Roof_R", "Wall_L", "Wall_Window", "Wall_Door", "Wall_R", "Wall_M",
    };

    static readonly HashSet<string> SolidTiles = new HashSet<string>
    {
        "Tree_Round", "Tree_Pine", "Bush", "BigRock", "Stump", "Fence_H", "Fence_V", "Cave_Wall", "Crystal",
        "Roof_L", "Roof_M", "Roof_R", "Wall_L", "Wall_Window", "Wall_Door", "Wall_R", "Wall_M",
        "TallGrass_A", "TallGrass_B",   // hanya dipakai di TM_TallGrass (collider Is Trigger)
    };

    static Dictionary<string, Tile> tiles;
    static RuleTile rtPath;
    static RuleTile rtWater;

    static AnimatorController acPlayer;
    static AnimatorController acMonster;
    static readonly Dictionary<string, AnimatorOverrideController> npcAocs = new Dictionary<string, AnimatorOverrideController>();
    static readonly Dictionary<string, AnimatorOverrideController> monsterAocs = new Dictionary<string, AnimatorOverrideController>();

    static PlayerConfig playerConfig;
    static MonsterDatabase database;
    static Dictionary<string, MonsterData> monsters;
    static DialogueData sariTips;

    // ---------------------------------------------------------------- P1: import pixel art

    static void ImportArt()
    {
        // P1 Praktikum 3 Langkah 2: preset disimpan SEBELUM sprite dipotong (Sprite Sheet kosong, 0 sprite)
        string playerSheet = ArtDir + "/Characters/Player_Walk_16x16.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(playerSheet);
        ApplyPixelSettings(importer, SpriteImportMode.Multiple, PivotBottom);
        importer.SaveAndReimport();
        SetSpriteRects(importer, new List<SpriteRect>());
        EnsureFolder(Root + "/Settings/Presets");
        var preset = new Preset(importer);
        AssetDatabase.CreateAsset(preset, Root + "/Settings/Presets/Pre_PixelSprite.preset");
        Preset.SetDefaultPresetsForType(new PresetType(importer),
            new[] { new DefaultPreset("glob:\"Assets/_Project/Art/**\"", preset) });

        foreach (string path in Pngs(ArtDir + "/Characters")) SliceGrid(path, 16, 16, PivotBottom, -1);
        foreach (string path in Pngs(ArtDir + "/Monsters"))
        {
            int size = FrameSize(path);
            SliceGrid(path, size, size, PivotBottom, -1);
        }
        SliceGrid(ArtDir + "/Tiles/Tiles_Overworld_16x16.png", 16, 16, PivotCenter, TileNames.Length);
        SliceGrid(ArtDir + "/VFX/VFX_HitSlash_4f_32x32.png", 32, 32, PivotCenter, -1);
        SliceGrid(ArtDir + "/VFX/VFX_Ring_4f_32x32.png", 32, 32, PivotCenter, -1);

        // Latar battle/judul, Square, VFX tunggal: Sprite Mode Single
        foreach (string path in Pngs(ArtDir + "/Backgrounds")) ImportSingle(path, PivotCenter, Vector4.zero);
        foreach (string path in Pngs(ArtDir + "/VFX").Where(p => !p.Contains("_4f_"))) ImportSingle(path, PivotCenter, Vector4.zero);
        foreach (string path in Pngs(ArtDir + "/UI"))
        {
            string file = Path.GetFileNameWithoutExtension(path);
            bool sliced = file.StartsWith("UI_Panel") || file.StartsWith("UI_Button") || file == "UI_Bar_Frame";
            Vector2 pivot = file == "Prop_SaveStone" ? PivotBottom : PivotCenter;
            ImportSingle(path, pivot, sliced ? new Vector4(4, 4, 4, 4) : Vector4.zero);   // panel: Border 4, Sliced
        }

        // Font Pixelify Sans (OFL): Hinted Raster, Character Dynamic
        foreach (string path in Directory.GetFiles(ArtDir + "/Fonts", "*.ttf"))
        {
            var font = (TrueTypeFontImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
            font.fontRenderingMode = FontRenderingMode.HintedRaster;
            font.fontTextureCase = FontTextureCase.Dynamic;
            font.SaveAndReimport();
        }
    }

    static int FrameSize(string path)
    {
        Match m = Regex.Match(Path.GetFileName(path), @"_(\d+)x\d+\.png$");
        return m.Success ? int.Parse(m.Groups[1].Value) : 16;
    }

    static void ApplyPixelSettings(TextureImporter ti, SpriteImportMode mode, Vector2 pivot)
    {
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = mode;
        ti.spritePixelsPerUnit = 16;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.wrapMode = TextureWrapMode.Clamp;
        var settings = new TextureImporterSettings();
        ti.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        settings.spriteGenerateFallbackPhysicsShape = true;   // physics shape dihitung saat import (build tidak perlu tekstur readable)
        ti.SetTextureSettings(settings);
    }

    static void ImportSingle(string path, Vector2 pivot, Vector4 border)
    {
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ApplyPixelSettings(ti, SpriteImportMode.Single, pivot);
        ti.spriteBorder = border;
        ti.SaveAndReimport();
    }

    static void SliceGrid(string path, int cellW, int cellH, Vector2 pivot, int maxCount)
    {
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ApplyPixelSettings(ti, SpriteImportMode.Multiple, pivot);
        ti.SaveAndReimport();
        ti.GetSourceTextureWidthAndHeight(out int width, out int height);

        string baseName = Path.GetFileNameWithoutExtension(path);
        var rects = new List<SpriteRect>();
        int cols = width / cellW, rows = height / cellH, index = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (maxCount >= 0 && index >= maxCount) break;
                rects.Add(new SpriteRect
                {
                    name = $"{baseName}_{index}",
                    rect = new Rect(c * cellW, height - (r + 1) * cellH, cellW, cellH),
                    alignment = SpriteAlignment.Custom,
                    pivot = pivot,
                    spriteID = GUID.Generate(),
                });
                index++;
            }
        }
        SetSpriteRects(ti, rects);
    }

    static void SetSpriteRects(TextureImporter ti, List<SpriteRect> rects)
    {
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(ti);
        provider.InitSpriteEditorDataProvider();
        provider.SetSpriteRects(rects.ToArray());
        var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        names?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
        provider.Apply();
        ti.SaveAndReimport();
    }

    // ---------------------------------------------------------------- P2: tile, rule tile, palette

    static void CreateTiles()
    {
        EnsureFolder(PaletteDir);
        Sprite[] sprites = Sprites(ArtDir + "/Tiles/Tiles_Overworld_16x16.png");
        if (sprites.Length < TileNames.Length) throw new Exception($"Slice tile hanya {sprites.Length}");

        tiles = new Dictionary<string, Tile>();
        for (int i = 0; i < TileNames.Length; i++)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprites[i];
            tile.colliderType = SolidTiles.Contains(TileNames[i]) ? Tile.ColliderType.Grid : Tile.ColliderType.None;
            AssetDatabase.CreateAsset(tile, $"{PaletteDir}/{TileNames[i]}.asset");
            tiles[TileNames[i]] = tile;
        }

        rtPath = MakeRuleTile("RT_Path", "Path", sprites, Tile.ColliderType.None);
        rtWater = MakeRuleTile("RT_Water", "Water", sprites, Tile.ColliderType.Grid);
        CreatePalette();
    }

    static Sprite TileSprite(Sprite[] sprites, string name) => sprites[Array.IndexOf(TileNames, name)];

    /// <summary>
    /// Isi hanya 4 kotak (atas, bawah, kiri, kanan) untuk 8 tepi; sisanya pakai sprite default (_C).
    /// Tantangan P2: 4 aturan sudut dalam (cek diagonal), dipasang untuk Water dan juga Path.
    /// </summary>
    static RuleTile MakeRuleTile(string assetName, string prefix, Sprite[] sprites, Tile.ColliderType collider)
    {
        const int This = RuleTile.TilingRuleOutput.Neighbor.This;
        const int Not = RuleTile.TilingRuleOutput.Neighbor.NotThis;
        Vector3Int up = Vector3Int.up, down = Vector3Int.down, left = Vector3Int.left, right = Vector3Int.right;
        var upLeft = new Vector3Int(-1, 1, 0);
        var upRight = new Vector3Int(1, 1, 0);
        var downLeft = new Vector3Int(-1, -1, 0);
        var downRight = new Vector3Int(1, -1, 0);

        var ruleTile = ScriptableObject.CreateInstance<RuleTile>();
        ruleTile.m_DefaultSprite = TileSprite(sprites, prefix + "_C");
        ruleTile.m_DefaultColliderType = collider;
        var rules = new List<RuleTile.TilingRule>();

        void Add(string suffix, params (Vector3Int pos, int neighbor)[] conditions)
        {
            var rule = new RuleTile.TilingRule
            {
                m_Id = rules.Count,
                m_Sprites = new[] { TileSprite(sprites, prefix + suffix) },
                m_ColliderType = collider,
                m_NeighborPositions = conditions.Select(c => c.pos).ToList(),
                m_Neighbors = conditions.Select(c => c.neighbor).ToList(),
            };
            rules.Add(rule);
        }

        Add("_iTL", (up, This), (down, This), (left, This), (right, This), (upLeft, Not));
        Add("_iTR", (up, This), (down, This), (left, This), (right, This), (upRight, Not));
        Add("_iBL", (up, This), (down, This), (left, This), (right, This), (downLeft, Not));
        Add("_iBR", (up, This), (down, This), (left, This), (right, This), (downRight, Not));
        Add("_TL", (up, Not), (left, Not), (down, This), (right, This));
        Add("_T", (up, Not), (left, This), (right, This), (down, This));
        Add("_TR", (up, Not), (right, Not), (down, This), (left, This));
        Add("_L", (left, Not), (up, This), (down, This), (right, This));
        Add("_R", (right, Not), (up, This), (down, This), (left, This));
        Add("_BL", (down, Not), (left, Not), (up, This), (right, This));
        Add("_B", (down, Not), (left, This), (right, This), (up, This));
        Add("_BR", (down, Not), (right, Not), (up, This), (left, This));

        ruleTile.m_TilingRules = rules;
        AssetDatabase.CreateAsset(ruleTile, $"{PaletteDir}/{assetName}.asset");
        return ruleTile;
    }

    /// <summary>Tile Palette Pal_Overworld: 56 tile + RT_Path + RT_Water di kolom kanan.</summary>
    static void CreatePalette()
    {
        try
        {
            Type utility = Type.GetType("UnityEditor.Tilemaps.GridPaletteUtility, Unity.2D.Tilemap.Editor");
            MethodInfo create = utility?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "CreateNewPalette" && m.GetParameters().Length == 6);
            if (create == null)
            {
                Debug.LogWarning("[DemoBuilder] GridPaletteUtility.CreateNewPalette tidak ditemukan, palette dilewati.");
                return;
            }
            ParameterInfo[] ps = create.GetParameters();
            object sizing = Enum.ToObject(ps[3].ParameterType, 0);   // Automatic
            var palette = (GameObject)create.Invoke(null, new object[]
            {
                PaletteDir, "Pal_Overworld", GridLayout.CellLayout.Rectangle, sizing, Vector3.one, GridLayout.CellSwizzle.XYZ,
            });
            string path = AssetDatabase.GetAssetPath(palette);
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            Tilemap tilemap = contents.GetComponentInChildren<Tilemap>();
            for (int i = 0; i < TileNames.Length; i++)
                tilemap.SetTile(new Vector3Int(i % 12, -(i / 12), 0), tiles[TileNames[i]]);
            tilemap.SetTile(new Vector3Int(13, 0, 0), rtPath);
            tilemap.SetTile(new Vector3Int(13, -1, 0), rtWater);
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DemoBuilder] Palette gagal dibuat: {e.Message}");
        }
    }

    // ---------------------------------------------------------------- P3/P4: animasi

    static void CreateAnimations()
    {
        EnsureFolder(AnimDir + "/Player");
        EnsureFolder(AnimDir + "/Npc");
        EnsureFolder(AnimDir + "/Monsters");

        Sprite[] player = Sprites(ArtDir + "/Characters/Player_Walk_16x16.png");
        AnimationClip[] playerClips = WalkClips(AnimDir + "/Player", "Anim_Player", player);
        acPlayer = BuildPlayerController(AnimDir + "/Player/AC_Player.controller", playerClips);

        // Tugas P3: Animator Override Controller dari AC_Player untuk NPC
        npcAocs.Clear();
        foreach (string npc in new[] { "Elder", "Girl", "Guard", "Merchant" })
        {
            Sprite[] sheet = Sprites($"{ArtDir}/Characters/NPC_{npc}_Walk_16x16.png");
            AnimationClip[] clips = WalkClips(AnimDir + "/Npc", $"Anim_Npc_{npc}", sheet);
            var aoc = new AnimatorOverrideController(acPlayer) { name = $"AOC_Npc_{npc}" };
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            for (int i = 0; i < playerClips.Length; i++)
                overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(playerClips[i], clips[i]));
            aoc.ApplyOverrides(overrides);
            AssetDatabase.CreateAsset(aoc, $"{AnimDir}/Npc/AOC_Npc_{npc}.overrideController");
            npcAocs[npc] = aoc;
        }

        // P4 Praktikum 3: clip idle 2 frame (Samples 3), satu AC_Monster, dua belas override
        var idleClips = new Dictionary<string, AnimationClip>();
        foreach (string path in Pngs(ArtDir + "/Monsters"))
        {
            string name = MonsterName(path);
            Sprite[] frames = Sprites(path);
            idleClips[name] = SpriteClip($"{AnimDir}/Monsters/Anim_Mon_{name}_Idle.anim", new[] { frames[0], frames[1] }, 3f, true);
        }

        acMonster = AnimatorController.CreateAnimatorControllerAtPath(AnimDir + "/Monsters/AC_Monster.controller");
        AnimatorState idle = acMonster.layers[0].stateMachine.AddState("Idle");
        idle.motion = idleClips["Pyrit"];
        acMonster.layers[0].stateMachine.defaultState = idle;
        EditorUtility.SetDirty(acMonster);

        monsterAocs.Clear();
        foreach (KeyValuePair<string, AnimationClip> kv in idleClips)
        {
            var aoc = new AnimatorOverrideController(acMonster) { name = $"AOC_Mon_{kv.Key}" };
            aoc[idleClips["Pyrit"]] = kv.Value;
            AssetDatabase.CreateAsset(aoc, $"{AnimDir}/Monsters/AOC_Mon_{kv.Key}.overrideController");
            monsterAocs[kv.Key] = aoc;
        }
    }

    static string MonsterName(string path)
    {
        // Pyro_1_Pyrit_16x16.png -> Pyrit
        string[] parts = Path.GetFileNameWithoutExtension(path).Split('_');
        return parts.Length >= 3 ? parts[2] : parts[0];
    }

    /// <summary>
    /// 8 clip: Idle (Down _0, Left _3, Right _6, Up _9) lalu Walk (_0,_1,_0,_2 / _3,_4,_3,_5 / _6,_7,_6,_8 / _9,_10,_9,_11), Samples 8.
    /// </summary>
    static AnimationClip[] WalkClips(string folder, string prefix, Sprite[] s)
    {
        string[] dirs = { "Down", "Left", "Right", "Up" };
        var clips = new AnimationClip[8];
        for (int d = 0; d < 4; d++)
        {
            int b = d * 3;
            clips[d] = SpriteClip($"{folder}/{prefix}_Idle_{dirs[d]}.anim", new[] { s[b] }, 8f, true);
            clips[d + 4] = SpriteClip($"{folder}/{prefix}_Walk_{dirs[d]}.anim", new[] { s[b], s[b + 1], s[b], s[b + 2] }, 8f, true);
        }
        return clips;
    }

    static AnimationClip SpriteClip(string path, Sprite[] frames, float sampleRate, bool loop)
    {
        var clip = new AnimationClip { frameRate = sampleRate };
        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / sampleRate, value = frames[i] };
        keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / sampleRate, value = frames[frames.Length - 1] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    /// <summary>AC_Player: parameter MoveX, MoveY, IsMoving (+ Speed), state Idle &amp; Walk berisi Blend Tree 2D Simple Directional.</summary>
    static AnimatorController BuildPlayerController(string path, AnimationClip[] c)
    {
        AnimatorController ac = AnimatorController.CreateAnimatorControllerAtPath(path);
        ac.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        ac.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        ac.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        ac.AddParameter(new AnimatorControllerParameter { name = "Speed", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });

        AnimatorStateMachine sm = ac.layers[0].stateMachine;
        AnimatorState idle = ac.CreateBlendTreeInController("Idle", out BlendTree idleTree, 0);
        Setup2D(idleTree, c[0], c[1], c[2], c[3]);
        AnimatorState walk = ac.CreateBlendTreeInController("Walk", out BlendTree walkTree, 0);
        Setup2D(walkTree, c[4], c[5], c[6], c[7]);
        walk.speedParameterActive = true;
        walk.speedParameter = "Speed";
        sm.defaultState = idle;

        AnimatorStateTransition toWalk = idle.AddTransition(walk);
        toWalk.hasExitTime = false;
        toWalk.duration = 0f;
        toWalk.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");

        AnimatorStateTransition toIdle = walk.AddTransition(idle);
        toIdle.hasExitTime = false;
        toIdle.duration = 0f;
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");

        EditorUtility.SetDirty(ac);
        return ac;
    }

    static void Setup2D(BlendTree tree, AnimationClip down, AnimationClip left, AnimationClip right, AnimationClip up)
    {
        tree.blendType = BlendTreeType.SimpleDirectional2D;
        tree.blendParameter = "MoveX";
        tree.blendParameterY = "MoveY";
        tree.AddChild(down, new Vector2(0f, -1f));
        tree.AddChild(left, new Vector2(-1f, 0f));
        tree.AddChild(right, new Vector2(1f, 0f));
        tree.AddChild(up, new Vector2(0f, 1f));
    }

    // ---------------------------------------------------------------- P2/P4/P6: data

    static string SkillFor(Element element)
    {
        switch (element)
        {
            case Element.Pyro: return "Semburan Api";
            case Element.Aqua: return "Tembakan Air";
            case Element.Sprout: return "Daun Tajam";
            default: return "Setrum Kilat";
        }
    }

    static void CreateData()
    {
        EnsureFolder(DataDir + "/Monsters");
        EnsureFolder(DataDir + "/Dialogue");

        playerConfig = ScriptableObject.CreateInstance<PlayerConfig>();   // WalkSpeed 3,75, RunSpeed 6
        AssetDatabase.CreateAsset(playerConfig, DataDir + "/Cfg_Player.asset");

        string[] lines = File.ReadAllLines(DataDir + "/MonsterDatabase.csv");
        string[] header = lines[0].Trim().Split(',');
        int Col(string name) => Array.IndexOf(header, name);

        var rows = lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim().Split(',')).ToList();
        monsters = new Dictionary<string, MonsterData>();
        foreach (string[] f in rows)
        {
            var data = ScriptableObject.CreateInstance<MonsterData>();
            AssetDatabase.CreateAsset(data, $"{DataDir}/Monsters/Mon_{f[Col("name")]}.asset");
            monsters[f[Col("name")]] = data;
        }

        foreach (string[] f in rows)
        {
            string name = f[Col("name")];
            var element = (Element)Enum.Parse(typeof(Element), f[Col("element")]);
            MonsterData data = monsters[name];
            string evolvesTo = f[Col("evolvesTo")];
            Set(data, "id", f[Col("id")]);
            Set(data, "displayName", name);
            Set(data, "element", element);
            Set(data, "stage", int.Parse(f[Col("stage")]));
            Set(data, "frames", Sprites($"{ArtDir}/Monsters/{f[Col("spriteFile")]}"));
            Set(data, "baseHP", int.Parse(f[Col("baseHP")]));
            Set(data, "baseATK", int.Parse(f[Col("baseATK")]));
            Set(data, "baseDEF", int.Parse(f[Col("baseDEF")]));
            Set(data, "baseSPD", int.Parse(f[Col("baseSPD")]));
            Set(data, "skillName", SkillFor(element));
            Set(data, "evolvesTo", string.IsNullOrEmpty(evolvesTo) ? null : monsters[evolvesTo]);
            Set(data, "evolveLevel", int.TryParse(f[Col("evolveLevel")], out int level) ? level : 0);
            Set(data, "idleController", monsterAocs[name]);
            EditorUtility.SetDirty(data);
        }

        database = ScriptableObject.CreateInstance<MonsterDatabase>();
        AssetDatabase.CreateAsset(database, DataDir + "/MonsterDatabase.asset");
        Set(database, "monsters", rows.Select(f => (Object)monsters[f[Col("name")]]).ToList());

        sariTips = ScriptableObject.CreateInstance<DialogueData>();
        AssetDatabase.CreateAsset(sariTips, DataDir + "/Dialogue/Dlg_Sari_Tips.asset");
        Set(sariTips, "lines", new[]
        {
            "Mau tahu rahasia battle? Setiap monster punya elemen: Pyro, Aqua, Sprout, dan Volt.",
            "Pyro kuat melawan Sprout, Aqua kuat melawan Pyro, Sprout kuat melawan Aqua.",
            "Volt juga kuat melawan Aqua. Pakai Jurus elemen yang tepat, kerusakannya 1,5 kali lipat!",
        });
        AssetDatabase.SaveAssets();
    }
}
