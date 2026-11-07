using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// Scn_World (indeks 2): peta 40 x 36 tile, empat area.
/// Desa Awal (selatan: 3 rumah, kolam, pagar y = 11 celah x 19-20), Hutan Rimbun (tengah: pohon, 2 petak rumput tinggi),
/// Danau Sunyi (barat laut), Gua Petir (timur laut: lantai gua, dinding, kristal, Raijugar).
/// </summary>
public static partial class DemoBuilder
{
    const int MapW = 40;
    const int MapH = 36;

    static readonly Vector2Int[] CrystalCells =
    {
        new Vector2Int(24, 27), new Vector2Int(36, 28), new Vector2Int(26, 32), new Vector2Int(35, 32), new Vector2Int(33, 27),
    };

    static void BuildWorldScene()
    {
        Scene scene = NewScene();
        PlanMap(out string[,] ground, out string[,] decor, out string[,] block, out string[,] tall);

        // Satu Grid, empat Tilemap, empat tugas
        GameObject grid = NewObject("Grid");
        grid.AddComponent<Grid>();
        Tilemap tmGround = NewTilemap("TM_Ground", grid.transform, "Ground", 0, TilemapRenderer.Mode.Chunk);
        Tilemap tmDecor = NewTilemap("TM_Decor", grid.transform, "Decor", 0, TilemapRenderer.Mode.Chunk);
        Tilemap tmBlocking = NewTilemap("TM_Blocking", grid.transform, "Characters", 0, TilemapRenderer.Mode.Individual);
        Tilemap tmTall = NewTilemap("TM_TallGrass", grid.transform, "Decor", 1, TilemapRenderer.Mode.Chunk);
        Paint(tmGround, ground);
        Paint(tmDecor, decor);
        Paint(tmBlocking, block);
        Paint(tmTall, tall);

        // Tabrakan: Tilemap Collider 2D (Merge) + Composite Collider 2D (Rigidbody 2D Static)
        var tilemapCollider = tmBlocking.gameObject.AddComponent<TilemapCollider2D>();
        tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
        var composite = tmBlocking.gameObject.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        tmBlocking.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        var tallCollider = tmTall.gameObject.AddComponent<TilemapCollider2D>();
        tallCollider.isTrigger = true;

        // Kamera: Pixel Perfect 320 x 180 + Cinemachine (CM_Follow, Confiner 2D, CameraBounds)
        Camera cam = NewCamera(null, new Vector3(16.5f, 5.5f, -10f), Color.black, true);
        cam.gameObject.AddComponent<CinemachineBrain>();

        GameObject bounds = NewObject("CameraBounds");
        bounds.layer = LayerMask.NameToLayer("CameraBounds");
        var boundsShape = bounds.AddComponent<PolygonCollider2D>();
        boundsShape.pathCount = 1;
        boundsShape.SetPath(0, new[] { new Vector2(0, 0), new Vector2(MapW, 0), new Vector2(MapW, MapH), new Vector2(0, MapH) });
        boundsShape.isTrigger = true;

        Light2D globalLight = NewGlobalLight(1f);

        // Player + interaksi + cahaya pemain (P7)
        GameObject player = BuildPlayer(new Vector2(16.5f, 5.4f), out PlayerMovement movement);
        var interactor = player.AddComponent<PlayerInteractor>();
        Set(interactor, "movement", movement);
        Set(interactor, "interactableMask", (LayerMask)LayerMask.GetMask("Interactable"));
        Light2D playerLight = NewPointLight("PlayerLight", player.transform, new Vector3(0f, 0.5f, 0f), 1.2f, 3.6f,
            new Color(1f, 0.9f, 0.72f), 0f);

        GameObject follow = NewObject("CM_Follow");
        var vcam = follow.AddComponent<CinemachineCamera>();
        vcam.Follow = player.transform;
        LensSettings lens = vcam.Lens;
        lens.OrthographicSize = 5.625f;     // bawaan 10 merusak Confiner
        lens.NearClipPlane = 0.3f;
        lens.FarClipPlane = 1000f;
        vcam.Lens = lens;
        var composer = follow.AddComponent<CinemachinePositionComposer>();
        composer.CameraDistance = 10f;
        composer.Damping = new Vector3(0.5f, 0.5f, 0f);
        follow.AddComponent<CinemachinePixelPerfect>();
        var confiner = follow.AddComponent<CinemachineConfiner2D>();
        confiner.BoundingShape2D = boundsShape;

        // Partner pengikut + partikel
        SpriteRenderer partnerBody = NewSprite("Partner", null, new Vector3(15.8f, 5.5f, 0f), null, "Characters");
        var follower = partnerBody.gameObject.AddComponent<PartnerFollower>();
        Set(follower, "body", partnerBody);

        var burst = NewObject("PixelBurst").AddComponent<PixelBurst>();
        Set(burst, "pixel", spPixel);

        var rustle = player.AddComponent<GrassRustle>();
        Set(rustle, "tallGrass", tmTall);
        Set(rustle, "movement", movement);
        Set(rustle, "burst", burst);

        // Empat warga desa (varian Pfb_Npc)
        Transform npcRoot = NewObject("NPC").transform;
        PlacePrefab(npcPrefabs["Elder"], "Tetua Rahmat", new Vector3(25.5f, 7f, 0f), npcRoot);
        PlacePrefab(npcPrefabs["Girl"], "Sari", new Vector3(7f, 7f, 0f), npcRoot);
        PlacePrefab(npcPrefabs["Guard"], "Penjaga Batas", new Vector3(20f, 11.4f, 0f), npcRoot);
        PlacePrefab(npcPrefabs["Merchant"], "Pak Dagang", new Vector3(13f, 4f, 0f), npcRoot);

        // Batu Simpan di samping Tetua
        SpriteRenderer stoneBody = NewSprite("Batu Simpan", null, new Vector3(22.5f, 7f, 0f),
            LoadSprite(ArtDir + "/UI/Prop_SaveStone.png"), "Characters");
        stoneBody.gameObject.layer = LayerMask.NameToLayer("Interactable");
        var stoneCollider = stoneBody.gameObject.AddComponent<BoxCollider2D>();
        stoneCollider.size = new Vector2(0.8f, 0.5f);
        stoneCollider.offset = new Vector2(0f, 0.25f);
        var stone = stoneBody.gameObject.AddComponent<SaveStone>();
        Set(stone, "saveClip", Sfx("SFX_Save"));

        // Palang Gate_Hutan di celah pagar (hilang setelah has_starter)
        GameObject gate = NewObject("Gate");
        var toggle = gate.AddComponent<FlagToggle>();
        GameObject gateHutan = NewObject("Gate_Hutan", gate.transform, new Vector3(20f, 11.5f, 0f));
        var gateCollider = gateHutan.AddComponent<BoxCollider2D>();
        gateCollider.size = new Vector2(2f, 1f);
        NewSprite("Palang_Kiri", gateHutan.transform, new Vector3(-0.5f, 0f, 0f), tiles["Fence_H"].sprite, "Characters");
        NewSprite("Palang_Kanan", gateHutan.transform, new Vector3(0.5f, 0f, 0f), tiles["Fence_H"].sprite, "Characters");
        Set(toggle, "flagKey", StoryFlags.HasStarter);
        Set(toggle, "hideWhenSet", gateHutan);

        // Empat AreaZone (Box Collider 2D trigger)
        Transform areaRoot = NewObject("Areas").transform;
        AudioClip village = Bgm("BGM_Village");
        AudioClip forest = Bgm("BGM_Forest");
        NewArea(areaRoot, "Area_Desa", new Vector2(20f, 5.75f), new Vector2(40f, 11.5f), "Desa Awal", village);
        NewArea(areaRoot, "Area_Hutan", new Vector2(20f, 18.25f), new Vector2(40f, 13.5f), "Hutan Rimbun", forest);
        NewArea(areaRoot, "Area_Danau", new Vector2(11f, 30.5f), new Vector2(22f, 11f), "Danau Sunyi", forest);
        AreaZone cave = NewArea(areaRoot, "Area_Gua", new Vector2(31f, 30.5f), new Vector2(18f, 11f), "Gua Petir", forest);

        // P7: lampu kristal
        Transform caveLights = NewObject("CaveLights").transform;
        var extraLights = new List<Light2D>();
        foreach (Vector2Int c in CrystalCells)
            extraLights.Add(NewPointLight($"Crystal_{c.x}_{c.y}", caveLights, new Vector3(c.x + 0.5f, c.y + 0.5f, 0f),
                0.3f, 1.9f, new Color(1f, 0.95f, 0.62f), 0f));

        // P4: 8 monster liar + 1 tambahan di Danau Sunyi (tugas) + boss Raijugar
        Transform wildRoot = NewObject("WildMonsters").transform;
        PlaceWild(wildRoot, "Sprig", 5.5f, 16.5f, 2, 4);      // rumput barat Lv 2-4
        PlaceWild(wildRoot, "Zapi", 8.5f, 17.5f, 2, 4);
        PlaceWild(wildRoot, "Blubi", 6.5f, 14.5f, 2, 4);
        PlaceWild(wildRoot, "Pyrit", 9.5f, 15.5f, 2, 4);
        PlaceWild(wildRoot, "Pyrago", 26.5f, 16.5f, 5, 7);    // rumput timur Lv 5-7
        PlaceWild(wildRoot, "Finnox", 30.5f, 17.5f, 5, 7);
        PlaceWild(wildRoot, "Voltcat", 28.5f, 14.5f, 5, 7);
        PlaceWild(wildRoot, "Bloomlet", 31.5f, 15.5f, 5, 7);
        PlaceWild(wildRoot, "Blubi", 17.5f, 28.5f, 4, 6, false, "Wild_Blubi_Danau");   // tugas P4: monster liar di Danau Sunyi
        WildMonster boss = PlaceWild(wildRoot, "Raijugar", 30.5f, 31.5f, 9, 9, true);
        Light2D bossLight = NewPointLight("BossLight", boss.transform, new Vector3(0f, 0.9f, 0f), 0.3f, 1.8f,
            new Color(1f, 0.8f, 0.25f), 0f);
        Set(boss, "bossExtras", new Object[] { bossLight.gameObject });
        extraLights.Add(bossLight);

        var lightZone = cave.gameObject.AddComponent<LightZone>();
        Set(lightZone, "globalLight", globalLight);
        Set(lightZone, "playerLight", playerLight);
        Set(lightZone, "extraLights", extraLights);
        Set(lightZone, "insideIntensity", 0.15f);
        Set(lightZone, "fadeSpeed", 2f);

        // HUD dunia versi demo
        PartnerHud hud = BuildWorldHud();

        var installer = NewObject("WorldInstaller").AddComponent<WorldInstaller>();
        Set(installer, "player", movement);
        Set(installer, "interactor", interactor);
        Set(installer, "follower", follower);
        Set(installer, "hud", hud);
        Set(installer, "burst", burst);
        Set(installer, "respawnPoint", new Vector2(21.5f, 6f));

        SaveScene(scene, WorldScenePath);
    }

    static Tilemap NewTilemap(string name, Transform grid, string sortingLayer, int order, TilemapRenderer.Mode mode)
    {
        GameObject go = NewObject(name, grid);
        var tilemap = go.AddComponent<Tilemap>();
        var renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = order;
        renderer.mode = mode;
        return tilemap;
    }

    static void Paint(Tilemap tilemap, string[,] layer)
    {
        for (int x = 0; x < MapW; x++)
        {
            for (int y = 0; y < MapH; y++)
            {
                string name = layer[x, y];
                if (name == null) continue;
                TileBase tile = name == "RT_Path" ? rtPath : name == "RT_Water" ? (TileBase)rtWater : tiles[name];
                tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }
        tilemap.CompressBounds();
    }

    static AreaZone NewArea(Transform parent, string name, Vector2 center, Vector2 size, string areaName, AudioClip music)
    {
        GameObject go = NewObject(name, parent, center);
        var box = go.AddComponent<BoxCollider2D>();
        box.size = size;
        box.isTrigger = true;
        var zone = go.AddComponent<AreaZone>();
        Set(zone, "areaName", areaName);
        Set(zone, "music", music);
        return zone;
    }

    static WildMonster PlaceWild(Transform parent, string species, float x, float y, int minLevel, int maxLevel,
                                 bool boss = false, string objectName = null)
    {
        GameObject go = PlacePrefab(pfbWild, objectName ?? (boss ? $"Boss_{species}" : $"Wild_{species}"), new Vector3(x, y, 0f), parent);
        var wild = go.GetComponent<WildMonster>();
        MonsterData data = monsters[species];
        Set(wild, "species", data);
        Set(wild, "minLevel", minLevel);
        Set(wild, "maxLevel", maxLevel);
        Set(wild, "isBoss", boss);
        Set(wild, "stationary", boss);
        go.GetComponent<SpriteRenderer>().sprite = data.Frames[0];
        go.GetComponent<Animator>().runtimeAnimatorController = monsterAocs[species];
        if (boss)
        {
            go.transform.Find("Alert").localPosition = new Vector3(0f, 2.2f, 0f);   // boss +2,2
            var body = go.GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;                             // boss diam, tidak bisa didorong
            var box = go.GetComponent<BoxCollider2D>();
            box.size = new Vector2(1.4f, 0.6f);
            box.offset = new Vector2(0f, 0.3f);
        }
        return wild;
    }

    static PartnerHud BuildWorldHud()
    {
        Canvas canvas = NewCanvas("Canvas_HUD", 10);
        var hud = canvas.gameObject.AddComponent<PartnerHud>();

        // Panel partner 4, 4, 112, 34 - potret 8, 9, 24 x 24 - bar HP 36, 18, 74 x 6 / angka kanan 110 - bar EXP 36, 32, 74 x 3
        Image partnerPanel = NewPanel("PartnerPanel", canvas.transform, 4, 4, 112, 34, spNavy);
        Image portrait = NewImage("Portrait", partnerPanel.transform, 8, 9, 24, 24, monsters["Pyrit"].Frames[0], Color.white);
        portrait.preserveAspect = true;
        Text name = NewText("Name", partnerPanel.transform, 36, 6, 50, 10, "Pyrit", 8, UiTheme.Text, TextAnchor.MiddleLeft);
        Text level = NewText("Level", partnerPanel.transform, 80, 6, 30, 10, "Lv3", 8, UiTheme.Gold, TextAnchor.MiddleRight);
        Image hp = NewBar("HpBar", partnerPanel.transform, 36, 18, 74, 6, HpColors.High);
        Text hpText = NewText("HpText", partnerPanel.transform, 50, 24, 60, 8, "54/54", 7, UiTheme.Muted, TextAnchor.MiddleRight);
        Image exp = NewBar("ExpBar", partnerPanel.transform, 36, 32, 74, 3, UiTheme.Exp);

        // Panel area 226, 4, 90, 24
        Image areaPanel = NewPanel("AreaPanel", canvas.transform, 226, 4, 90, 24, spNavy);
        Text area = NewText("AreaName", areaPanel.transform, 231, 6, 82, 10, "Desa Awal", 8, UiTheme.Gold, TextAnchor.MiddleLeft);
        Text potions = NewText("Potions", areaPanel.transform, 231, 16, 82, 9, "Ramuan ×3", 7, UiTheme.Text, TextAnchor.MiddleLeft);

        Set(hud, "partnerPanel", partnerPanel.gameObject);
        Set(hud, "portrait", portrait);
        Set(hud, "nameText", name);
        Set(hud, "levelText", level);
        Set(hud, "hpText", hpText);
        Set(hud, "hpFill", hp);
        Set(hud, "expFill", exp);
        Set(hud, "areaText", area);
        Set(hud, "potionsText", potions);
        return hud;
    }

    // ---------------------------------------------------------------- rencana peta 40 x 36

    static void PlanMap(out string[,] ground, out string[,] decor, out string[,] block, out string[,] tall)
    {
        var rng = new System.Random(20261107);
        var g = new string[MapW, MapH];
        var d = new string[MapW, MapH];
        var b = new string[MapW, MapH];
        var t = new string[MapW, MapH];
        var keep = new bool[MapW, MapH];   // jangan diberi pohon acak
        var soft = new bool[MapW, MapH];   // penghalang yang boleh dihapus bila menutup jalan

        bool In(int x, int y) => x >= 0 && x < MapW && y >= 0 && y < MapH;
        string RandomTree() => rng.NextDouble() < 0.6 ? "Tree_Round" : "Tree_Pine";

        void Keep(int x0, int y0, int x1, int y1, int margin)
        {
            for (int x = x0 - margin; x <= x1 + margin; x++)
                for (int y = y0 - margin; y <= y1 + margin; y++)
                    if (In(x, y)) keep[x, y] = true;
        }

        // 1. rumput
        for (int x = 0; x < MapW; x++)
            for (int y = 0; y < MapH; y++)
                g[x, y] = rng.NextDouble() < 0.25 ? "Grass_B" : "Grass_A";

        // 2. jalan (RT_Path)
        void PathRect(int x0, int y0, int x1, int y1)
        {
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++) g[x, y] = "RT_Path";
            Keep(x0, y0, x1, y1, 1);
        }
        PathRect(2, 5, 37, 6);       // jalan desa bawah
        PathRect(19, 5, 20, 23);     // x 19-20 menerus dari desa ke hutan (lewat celah pagar)
        PathRect(6, 22, 34, 23);     // jalan hutan atas
        PathRect(8, 24, 9, 25);      // ke Danau Sunyi
        PathRect(30, 24, 31, 24);    // ke Gua Petir

        // 3. air (RT_Water di TM_Blocking): Danau Sunyi + kolam desa
        void Water(int x0, int y0, int x1, int y1)
        {
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++) b[x, y] = "RT_Water";
            Keep(x0, y0, x1, y1, 1);
        }
        Water(2, 26, 15, 33);
        Water(2, 34, 12, 34);
        Water(31, 7, 35, 10);
        for (int x = 2; x <= 15; x++)
            if (g[x, 25] != "RT_Path") g[x, 25] = "Sand";

        // 4. Gua Petir: lantai, dinding, pintu, kristal
        for (int x = 22; x <= 38; x++)
        {
            for (int y = 25; y <= 34; y++)
            {
                g[x, y] = rng.NextDouble() < 0.5 ? "Cave_Floor_A" : "Cave_Floor_B";
                keep[x, y] = true;
                if (x == 22 || x == 38 || y == 25 || y == 34) b[x, y] = "Cave_Wall";
            }
        }
        foreach (int x in new[] { 30, 31 })
        {
            b[x, 25] = null;
            d[x, 25] = "Cave_Entrance";
        }
        foreach (Vector2Int c in CrystalCells) b[c.x, c.y] = "Crystal";

        // 5. pagar desa y = 11 dari x = 1 sampai 38, celah x = 19 dan 20
        for (int x = 1; x <= 38; x++)
            if (x != 19 && x != 20) b[x, 11] = "Fence_H";
        for (int x = 2; x <= 14; x++) b[x, 3] = "Fence_H";   // pagar kebun bawah

        // 6. tiga rumah 3 x 2 (atap y = 9, dinding + pintu y = 8)
        void House(int x0)
        {
            b[x0, 9] = "Roof_L";
            b[x0 + 1, 9] = "Roof_M";
            b[x0 + 2, 9] = "Roof_R";
            b[x0, 8] = "Wall_L";
            b[x0 + 1, 8] = "Wall_Door";
            b[x0 + 2, 8] = "Wall_R";
        }
        House(4);
        House(9);
        House(24);

        // 7. pohon di tepi peta
        for (int x = 0; x < MapW; x++)
        {
            b[x, 0] = RandomTree();
            b[x, MapH - 1] = RandomTree();
        }
        for (int y = 0; y < MapH; y++)
        {
            b[0, y] = RandomTree();
            b[MapW - 1, y] = RandomTree();
        }

        // 8. dua petak rumput tinggi (TM_TallGrass)
        void Tall(int x0, int y0, int x1, int y1)
        {
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++) t[x, y] = rng.NextDouble() < 0.5 ? "TallGrass_A" : "TallGrass_B";
            Keep(x0, y0, x1, y1, 1);
        }
        Tall(4, 13, 10, 17);      // rumput barat
        Tall(25, 13, 32, 17);     // rumput timur

        // 9. area bebas pohon acak
        Keep(1, 1, 38, 10, 0);    // desa (pohon desa ditaruh manual)
        Keep(17, 12, 22, 23, 0);  // koridor dari celah pagar
        Keep(16, 26, 19, 30, 0);  // rumah monster danau

        // 10. hutan: pohon, semak, batu acak
        for (int x = 1; x < MapW - 1; x++)
        {
            for (int y = 12; y < MapH - 1; y++)
            {
                if (keep[x, y] || b[x, y] != null) continue;
                double r = rng.NextDouble();
                if (r < 0.2) b[x, y] = RandomTree();
                else if (r < 0.225) b[x, y] = "Bush";
                else if (r < 0.235) b[x, y] = rng.NextDouble() < 0.5 ? "BigRock" : "Stump";
                if (b[x, y] != null) soft[x, y] = true;
            }
        }

        // pohon & semak dekorasi desa
        var villageTrees = new[]
        {
            new Vector2Int(2, 8), new Vector2Int(2, 9), new Vector2Int(14, 9), new Vector2Int(16, 9), new Vector2Int(28, 9),
            new Vector2Int(29, 8), new Vector2Int(37, 9), new Vector2Int(37, 8), new Vector2Int(17, 2), new Vector2Int(27, 2),
            new Vector2Int(33, 2), new Vector2Int(36, 3),
        };
        foreach (Vector2Int c in villageTrees)
        {
            if (b[c.x, c.y] != null || g[c.x, c.y] == "RT_Path") continue;
            b[c.x, c.y] = c.y == 2 ? "Bush" : "Tree_Round";
            soft[c.x, c.y] = true;
        }

        // 11. dekorasi TM_Decor: bunga, batu kecil, papan
        for (int x = 1; x < MapW - 1; x++)
        {
            for (int y = 1; y < MapH - 1; y++)
            {
                if (b[x, y] != null || t[x, y] != null || d[x, y] != null) continue;
                if (g[x, y] != "Grass_A" && g[x, y] != "Grass_B") continue;
                double r = rng.NextDouble();
                if (r < 0.035) d[x, y] = "Flowers";
                else if (r < 0.06) g[x, y] = "Grass_Flower";
                else if (r < 0.07) d[x, y] = "SmallRock";
            }
        }
        foreach (Vector2Int c in new[] { new Vector2Int(17, 7), new Vector2Int(22, 12), new Vector2Int(11, 24) })
        {
            if (b[c.x, c.y] == null && g[c.x, c.y] != "RT_Path") d[c.x, c.y] = "Sign";
        }

        // 12. semua tujuan penting harus bisa dicapai dari titik awal
        var start = new Vector2Int(16, 5);
        var targets = new[]
        {
            new Vector2Int(25, 6), new Vector2Int(22, 6), new Vector2Int(7, 6), new Vector2Int(13, 5),     // Tetua, Batu Simpan, Sari, Pak Dagang
            new Vector2Int(20, 12),                                                                          // utara celah pagar
            new Vector2Int(5, 16), new Vector2Int(8, 17), new Vector2Int(6, 14), new Vector2Int(9, 15),      // rumput barat
            new Vector2Int(26, 16), new Vector2Int(30, 17), new Vector2Int(28, 14), new Vector2Int(31, 15),  // rumput timur
            new Vector2Int(9, 24), new Vector2Int(17, 28), new Vector2Int(30, 24),                          // danau, gua
            new Vector2Int(30, 26), new Vector2Int(30, 30), new Vector2Int(25, 28), new Vector2Int(36, 30),
            new Vector2Int(2, 20), new Vector2Int(37, 20), new Vector2Int(3, 13), new Vector2Int(36, 13),
        };
        foreach (Vector2Int target in targets)
        {
            if (FindPath(start, target, (x, y) => b[x, y] == null) != null) continue;
            List<Vector2Int> path = FindPath(start, target, (x, y) => b[x, y] == null || soft[x, y]);
            if (path == null)
            {
                Debug.LogWarning($"[DemoBuilder] Tujuan {target} tidak bisa dicapai.");
                continue;
            }
            foreach (Vector2Int c in path)
            {
                if (!soft[c.x, c.y]) continue;
                b[c.x, c.y] = null;
                soft[c.x, c.y] = false;
            }
        }

        ground = g;
        decor = d;
        block = b;
        tall = t;
    }

    static List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, Func<int, int, bool> walkable)
    {
        var previous = new Dictionary<Vector2Int, Vector2Int> { [start] = start };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        while (queue.Count > 0)
        {
            Vector2Int c = queue.Dequeue();
            if (c == goal)
            {
                var path = new List<Vector2Int>();
                for (Vector2Int p = goal; p != start; p = previous[p]) path.Add(p);
                path.Add(start);
                return path;
            }
            foreach (Vector2Int dir in dirs)
            {
                Vector2Int n = c + dir;
                if (n.x < 0 || n.y < 0 || n.x >= MapW || n.y >= MapH) continue;
                if (previous.ContainsKey(n) || !walkable(n.x, n.y)) continue;
                previous[n] = c;
                queue.Enqueue(n);
            }
        }
        return null;
    }
}
