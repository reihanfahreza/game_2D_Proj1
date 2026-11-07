using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static partial class DemoBuilder
{
    static void BuildScenes()
    {
        LoadUiAssets();
        BuildBootstrapScene();
        BuildTitleScene();
        BuildWorldScene();
        BuildBattleScene();
        BuildEndScene();
        BuildSandboxScene();
        SetSceneList();
    }

    // ---------------------------------------------------------------- Scn_Bootstrap (indeks 0)

    static void BuildBootstrapScene()
    {
        Scene scene = NewScene();

        GameObject boot = NewObject("GameBootstrap");
        var bootstrap = boot.AddComponent<GameBootstrap>();

        // Satu EventSystem saja, di Scn_Bootstrap (P5)
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        // AudioService (P6) + AudioListener tunggal
        GameObject audioGo = NewObject("AudioService");
        audioGo.AddComponent<AudioListener>();
        AudioSource musicA = NewSource(audioGo, true, 0f);
        AudioSource musicB = NewSource(audioGo, true, 0f);
        AudioSource sfx = NewSource(audioGo, false, 0.85f);
        var audio = audioGo.AddComponent<AudioService>();
        Set(audio, "musicA", musicA);
        Set(audio, "musicB", musicB);
        Set(audio, "sfx", sfx);
        Set(audio, "uiMove", Sfx("SFX_UI_Move"));
        Set(audio, "uiConfirm", Sfx("SFX_UI_Confirm"));
        Set(audio, "uiCancel", Sfx("SFX_UI_Cancel"));
        Set(audio, "uiError", Sfx("SFX_UI_Error"));
        Set(audio, "dialogBlip", Sfx("SFX_Dialog_Blip"));
        Set(audio, "encounter", Sfx("SFX_Encounter"));

        // Canvas_Dialogue (order 20): DialogueView + DialogueRunner
        Canvas dialogueCanvas = NewCanvas("Canvas_Dialogue", 20);
        RectTransform root = StretchGroup("DialogueRoot", dialogueCanvas.transform);
        Button clickCatcher = NewClickCatcher("ClickCatcher", root);
        Image box = NewPanel("Box", root, 6, 128, 308, 46, spNavy);
        Text body = NewText("Body", box.transform, 14, 134, 292, 34, "", 8, UiTheme.Text, TextAnchor.UpperLeft);
        Image continueHint = NewImage("ContinueHint", box.transform, 302, 162, 7, 5, spArrowDown, Color.white);
        Image speakerTag = NewPanel("SpeakerTag", root, 12, 117, 60, 11, spGold);
        Text speaker = NewText("Speaker", speakerTag.transform, 12, 117, 60, 11, "", 8, UiTheme.Panel, TextAnchor.MiddleCenter, true, false);
        Stretch(speaker.rectTransform);
        Image choicesPanel = NewPanel("Choices", root, 202, 80, 112, 44, spNavy);
        TextMenu choices = AttachMenu(choicesPanel.gameObject, choicesPanel.transform,
            new[] { new Vector2(210, 86), new Vector2(210, 98), new Vector2(210, 110) }, 100, 11, 8, 1);

        var view = dialogueCanvas.gameObject.AddComponent<DialogueView>();
        Set(view, "canvas", dialogueCanvas);
        Set(view, "root", root.gameObject);
        Set(view, "clickCatcher", clickCatcher);
        Set(view, "box", box);
        Set(view, "body", body);
        Set(view, "speakerTag", speakerTag.rectTransform);
        Set(view, "speaker", speaker);
        Set(view, "continueHint", continueHint);
        Set(view, "choices", choices);
        Set(view, "navySprite", spNavy);
        Set(view, "narrSprite", spNarr);
        var runner = dialogueCanvas.gameObject.AddComponent<DialogueRunner>();
        Set(runner, "view", view);
        root.gameObject.SetActive(false);

        // Canvas_Toast (order 30)
        Canvas toastCanvas = NewCanvas("Canvas_Toast", 30);
        var toastGroup = toastCanvas.gameObject.AddComponent<CanvasGroup>();
        toastGroup.alpha = 0f;
        toastGroup.blocksRaycasts = false;
        toastGroup.interactable = false;
        Image toastPanel = NewPanel("Panel", toastCanvas.transform, 100, 43, 120, 15, spGold);
        Text toastLabel = NewText("Label", toastPanel.transform, 100, 43, 120, 15, "", 8, UiTheme.Panel, TextAnchor.MiddleCenter, true, false);
        Stretch(toastLabel.rectTransform);
        var toast = toastCanvas.gameObject.AddComponent<ToastView>();
        Set(toast, "panel", toastPanel.rectTransform);
        Set(toast, "label", toastLabel);
        Set(toast, "group", toastGroup);

        // Canvas_Pause (order 40): layar redup + panel 104, 28, 112, 62
        Canvas pauseCanvas = NewCanvas("Canvas_Pause", 40);
        RectTransform pauseRoot = StretchGroup("PauseRoot", pauseCanvas.transform);
        NewFullImage("Dim", pauseRoot, spPixel, new Color(0f, 0f, 0f, 0.55f)).raycastTarget = true;
        Image pausePanel = NewPanel("Panel", pauseRoot, 104, 28, 112, 62, spNavy);
        NewText("Title", pausePanel.transform, 113, 32, 96, 10, "Jeda", 8, UiTheme.Gold, TextAnchor.MiddleLeft, true);
        TextMenu pauseList = NewMenu("Menu", pausePanel.transform,
            new[] { new Vector2(113, 47), new Vector2(113, 60), new Vector2(113, 73) }, 100, 11, 8, 1);
        var pause = pauseCanvas.gameObject.AddComponent<PauseMenu>();
        Set(pause, "root", pauseRoot.gameObject);
        Set(pause, "menu", pauseList);
        pauseRoot.gameObject.SetActive(false);

        // Canvas_Fader (order 100)
        Canvas faderCanvas = NewCanvas("Canvas_Fader", 100);
        var faderGroup = faderCanvas.gameObject.AddComponent<CanvasGroup>();
        faderGroup.alpha = 1f;
        NewFullImage("Black", faderCanvas.transform, spPixel, Color.black).raycastTarget = true;
        var fader = faderCanvas.gameObject.AddComponent<ScreenFader>();
        Set(fader, "group", faderGroup);

        Set(bootstrap, "firstScene", SceneFlow.TitleScene);
        Set(bootstrap, "monsterDatabase", database);
        Set(bootstrap, "fader", fader);
        Set(bootstrap, "dialogue", runner);
        Set(bootstrap, "toast", toast);
        Set(bootstrap, "audioService", audio);
        Set(bootstrap, "pauseMenu", pause);

        var autopilot = boot.AddComponent<DemoAutoPilot>();
        Set(autopilot, "bootstrap", bootstrap);

        SaveScene(scene, BootstrapScenePath);
    }

    static AudioSource NewSource(GameObject host, bool loop, float volume)
    {
        var source = host.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.volume = volume;
        source.spatialBlend = 0f;
        return source;
    }

    static TextMenu AttachMenu(GameObject host, Transform rowParent, Vector2[] rows, float rowW, float rowH, int px, int columns)
    {
        var menu = host.AddComponent<TextMenu>();
        var texts = new Text[rows.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            Text row = NewText($"Row_{i}", rowParent, rows[i].x, rows[i].y, rowW, rowH, $"Pilihan {i}", px, UiTheme.Text, TextAnchor.MiddleLeft);
            row.raycastTarget = true;
            var button = row.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            texts[i] = row;
        }
        Set(menu, "rows", texts);
        Set(menu, "columns", columns);
        return menu;
    }

    // ---------------------------------------------------------------- Scn_Title (indeks 1)

    static void BuildTitleScene()
    {
        Scene scene = NewScene();
        NewCamera(null, new Vector3(0f, 0f, -10f), UiTheme.Ink, false);
        var installer = NewObject("TitleInstaller").AddComponent<TitleInstaller>();

        Canvas canvas = NewCanvas("Canvas_Title", 0);
        RectTransform background = NewRect("Background", canvas.transform);
        background.anchorMin = background.anchorMax = background.pivot = new Vector2(0.5f, 0.5f);
        background.sizeDelta = new Vector2(440f * UiTheme.Px, 180f * UiTheme.Px);   // BG_Title_World 440 x 180
        background.anchoredPosition = Vector2.zero;
        var bgImage = background.gameObject.AddComponent<Image>();
        bgImage.sprite = LoadSprite(ArtDir + "/Backgrounds/BG_Title_World.png");
        bgImage.raycastTarget = false;
        Color overlay = UiTheme.Ink;
        overlay.a = 0.55f;                                                           // Overlay #121027 alpha 0,55
        NewFullImage("Overlay", canvas.transform, spPixel, overlay);

        NewText("TitlePulau", canvas.transform, 0, 20, 320, 14, "PULAU", 12, UiTheme.Cream, TextAnchor.MiddleCenter, true);
        Text big = NewText("TitleMonster", canvas.transform, 0, 33, 320, 34, "MONSTER", 28, UiTheme.Gold, TextAnchor.MiddleCenter, true, false);
        var outline = big.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color32(74, 36, 12, 255);
        outline.effectDistance = new Vector2(4f, -4f);

        string[] starterNames = { "Pyrit", "Blubi", "Sprig", "Zapi" };
        var starterImages = new Image[starterNames.Length];
        for (int i = 0; i < starterNames.Length; i++)
        {
            starterImages[i] = NewImage($"Starter_{starterNames[i]}", canvas.transform, 100 + i * 32, 70, 24, 24,
                monsters[starterNames[i]].Frames[0], Color.white);
            starterImages[i].preserveAspect = true;
        }

        TextMenu menu = NewMenu("Menu", canvas.transform,
            new[] { new Vector2(124, 104), new Vector2(124, 116), new Vector2(124, 128) }, 110, 11, 8, 1);
        NewText("Hint", canvas.transform, 0, 152, 320, 9, "Enter / Spasi untuk memilih · klik juga bisa", 7, UiTheme.Muted, TextAnchor.MiddleCenter);
        NewText("Credit", canvas.transform, 4, 167, 312, 10, $"{StudentName} · NIM {StudentNim}", 7, UiTheme.Text, TextAnchor.MiddleCenter);

        Set(installer, "menu", menu);
        Set(installer, "titleMusic", Bgm("BGM_Title"));
        Set(installer, "background", background);
        Set(installer, "starterImages", starterImages);
        Set(installer, "starters", starterNames.Select(n => (Object)monsters[n]).ToList());

        SaveScene(scene, TitleScenePath);
    }

    // ---------------------------------------------------------------- Scn_Battle (indeks 3)

    static void BuildBattleScene()
    {
        Scene scene = NewScene();
        GameObject rig = NewObject("CameraRig");
        Camera cam = NewCamera(rig.transform, new Vector3(0f, 0f, -10f), Color.black, true);
        NewGlobalLight(1f);

        SpriteRenderer background = NewSprite("Background", null, Vector3.zero,
            LoadSprite(ArtDir + "/Backgrounds/BG_Battle_Forest_320x180.png"), "Default", -10);
        Sprite soft = LoadSprite(ArtDir + "/VFX/VFX_SoftCircle_16.png");
        SpriteRenderer enemyShadow = NewSprite("EnemyShadow", null, new Vector3(4.5f, -0.125f, 0f), soft, "Default", -5);
        SpriteRenderer playerShadow = NewSprite("PlayerShadow", null, new Vector3(-4.5f, -2.375f, 0f), soft, "Default", -5);
        foreach (SpriteRenderer shadow in new[] { enemyShadow, playerShadow })
        {
            shadow.color = new Color(0f, 0f, 0f, 0.35f);
            shadow.transform.localScale = new Vector3(2.6f, 0.7f, 1f);
        }

        // EnemyMon: tengah x 232, kaki y 92 -> dunia (4,5; -0,125). PlayerMon: x 88, kaki y 128 -> (-4,5; -2,375), flipX
        SpriteRenderer enemyMon = NewSprite("EnemyMon", null, new Vector3(4.5f, -0.125f, 0f), monsters["Pyrit"].Frames[0], "Characters");
        SpriteRenderer playerMon = NewSprite("PlayerMon", null, new Vector3(-4.5f, -2.375f, 0f), monsters["Pyrit"].Frames[0], "Characters");
        playerMon.flipX = true;
        enemyMon.transform.localScale = new Vector3(3f, 3f, 1f);
        playerMon.transform.localScale = new Vector3(3f, 3f, 1f);

        GameObject installerGo = NewObject("BattleInstaller");
        var battle = installerGo.AddComponent<BattleSystem>();
        var battleAudio = installerGo.AddComponent<BattleAudio>();
        var feel = installerGo.AddComponent<BattleFeel>();
        var debugInput = installerGo.AddComponent<BattleDebugInput>();
        var installer = installerGo.AddComponent<BattleInstaller>();
        Set(debugInput, "battle", battle);

        var burst = NewObject("PixelBurst").AddComponent<PixelBurst>();
        Set(burst, "pixel", spPixel);
        var pool = NewObject("VfxPool").AddComponent<VfxPool>();
        Set(pool, "prefab", pfbSlash.GetComponent<SpriteVfx>());

        // Canvas_Battle: tata letak piksel demo x 4
        Canvas canvas = NewCanvas("Canvas_Battle", 0);

        Image enemyPanel = NewPanel("EnemyPanel", canvas.transform, 8, 8, 128, 30, spNavy);
        Text enemyName = NewText("Name", enemyPanel.transform, 13, 10, 80, 10, "Pyrit", 8, UiTheme.Text, TextAnchor.MiddleLeft);
        Text enemyLevel = NewText("Level", enemyPanel.transform, 96, 10, 36, 10, "Lv4", 8, UiTheme.Gold, TextAnchor.MiddleRight);
        Text enemyElement = NewText("Element", enemyPanel.transform, 13, 22, 30, 8, "Pyro", 7, UiTheme.Muted, TextAnchor.MiddleLeft);
        Image enemyHp = NewBar("HpBar", enemyPanel.transform, 44, 23, 88, 6, HpColors.High);
        var enemyPanelView = enemyPanel.gameObject.AddComponent<MonsterPanel>();
        Set(enemyPanelView, "nameText", enemyName);
        Set(enemyPanelView, "levelText", enemyLevel);
        Set(enemyPanelView, "elementText", enemyElement);
        Set(enemyPanelView, "hpFill", enemyHp);
        Set(enemyPanelView, "body", enemyMon);
        Set(enemyPanelView, "faceRight", false);

        Image playerPanel = NewPanel("PlayerPanel", canvas.transform, 186, 92, 128, 36, spNavy);
        Text playerName = NewText("Name", playerPanel.transform, 191, 94, 80, 10, "Pyrit", 8, UiTheme.Text, TextAnchor.MiddleLeft);
        Text playerLevel = NewText("Level", playerPanel.transform, 276, 94, 34, 10, "Lv3", 8, UiTheme.Gold, TextAnchor.MiddleRight);
        Image playerHp = NewBar("HpBar", playerPanel.transform, 191, 106, 119, 6, HpColors.High);
        Text playerHpText = NewText("HpText", playerPanel.transform, 230, 112, 80, 9, "54/54", 7, UiTheme.Muted, TextAnchor.MiddleRight);
        Image playerExp = NewBar("ExpBar", playerPanel.transform, 191, 123, 119, 3, UiTheme.Exp);
        Image star = NewImage("StarBadge", playerPanel.transform, 299, 82, 12, 12, spStar, Color.white);
        var playerPanelView = playerPanel.gameObject.AddComponent<MonsterPanel>();
        Set(playerPanelView, "nameText", playerName);
        Set(playerPanelView, "levelText", playerLevel);
        Set(playerPanelView, "hpText", playerHpText);
        Set(playerPanelView, "hpFill", playerHp);
        Set(playerPanelView, "expFill", playerExp);
        Set(playerPanelView, "body", playerMon);
        Set(playerPanelView, "faceRight", true);
        Set(playerPanelView, "starBadge", star.gameObject);

        Image messagePanel = NewPanel("MessagePanel", canvas.transform, 0, 132, 196, 48, spNavy);
        Text message = NewText("Message", messagePanel.transform, 9, 138, 181, 24, "", 8, UiTheme.Text, TextAnchor.UpperLeft);
        Text skillHint = NewText("SkillHint", messagePanel.transform, 9, 166, 181, 9, "", 7, UiTheme.Muted, TextAnchor.MiddleLeft);

        Image menuPanel = NewPanel("MenuPanel", canvas.transform, 196, 132, 124, 48, spNavy);
        TextMenu menu = NewMenu("Menu", menuPanel.transform,
            new[] { new Vector2(203, 140), new Vector2(261, 140), new Vector2(203, 160), new Vector2(261, 160) }, 58, 12, 8, 2);

        RectTransform fxLayer = StretchGroup("FxLayer", canvas.transform);
        var rings = new Image[3];
        for (int i = 0; i < rings.Length; i++)
        {
            RectTransform ring = NewRect($"Ring_{i}", fxLayer);
            ring.anchorMin = ring.anchorMax = ring.pivot = new Vector2(0.5f, 0.5f);
            ring.sizeDelta = new Vector2(64f, 64f);
            rings[i] = ring.gameObject.AddComponent<Image>();
            rings[i].sprite = spRing;
            rings[i].color = UiTheme.Gold;
            rings[i].raycastTarget = false;
        }
        Text damage = NewText("DamageTemplate", fxLayer, 0, 0, 40, 12, "12", 10, Color.white, TextAnchor.MiddleCenter, true);
        RectTransform damageRect = damage.rectTransform;
        damageRect.anchorMin = damageRect.anchorMax = damageRect.pivot = new Vector2(0.5f, 0.5f);
        damageRect.sizeDelta = new Vector2(200f, 48f);
        damage.gameObject.SetActive(false);

        Image flash = NewFullImage("Flash", canvas.transform, spPixel, new Color(1f, 1f, 1f, 0f));
        Image wipeTop = NewImage("WipeTop", canvas.transform, 0, 0, 320, 90, spPixel, UiTheme.Ink);
        Image wipeBottom = NewImage("WipeBottom", canvas.transform, 0, 90, 320, 90, spPixel, UiTheme.Ink);

        var view = canvas.gameObject.AddComponent<BattleView>();
        Set(view, "messageText", message);
        Set(view, "hintText", skillHint);
        Set(view, "menu", menu);
        Set(view, "enemyPanel", enemyPanelView);
        Set(view, "playerPanel", playerPanelView);
        Set(view, "rings", rings);
        Set(view, "flash", flash);
        Set(view, "wipeTop", wipeTop.rectTransform);
        Set(view, "wipeBottom", wipeBottom.rectTransform);
        Set(view, "damageLayer", fxLayer);
        Set(view, "damageTemplate", damage);
        Set(view, "worldCamera", cam);

        Set(battleAudio, "hit", Sfx("SFX_Hit"));
        Set(battleAudio, "hitCritical", Sfx("SFX_Hit_Critical"));
        Set(battleAudio, "miss", Sfx("SFX_Miss"));
        Set(battleAudio, "levelUp", Sfx("SFX_LevelUp"));
        Set(battleAudio, "evolve", Sfx("SFX_Evolve"));
        Set(battleAudio, "victory", Sfx("SFX_Victory_Jingle"));
        Set(battleAudio, "faint", Sfx("SFX_Faint"));
        Set(battleAudio, "heal", Sfx("SFX_Heal"));

        Set(feel, "cameraRig", rig.transform);
        Set(feel, "playerBody", playerMon);
        Set(feel, "enemyBody", enemyMon);
        Set(feel, "hitVfx", pool);
        Set(feel, "burst", burst);

        Set(installer, "battle", battle);
        Set(installer, "view", view);
        Set(installer, "background", background);
        Set(installer, "forestBackground", LoadSprite(ArtDir + "/Backgrounds/BG_Battle_Forest_320x180.png"));
        Set(installer, "caveBackground", LoadSprite(ArtDir + "/Backgrounds/BG_Battle_Cave_320x180.png"));
        Set(installer, "battleAudio", battleAudio);
        Set(installer, "feel", feel);
        Set(installer, "battleMusic", Bgm("BGM_Battle"));
        Set(installer, "bossMusic", Bgm("BGM_Boss"));

        SaveScene(scene, BattleScenePath);
    }

    // ---------------------------------------------------------------- Scn_End (indeks 4)

    static void BuildEndScene()
    {
        Scene scene = NewScene();
        NewCamera(null, new Vector3(0f, 0f, -10f), UiTheme.Ink, false);
        var end = NewObject("EndInstaller").AddComponent<EndInstaller>();

        Canvas canvas = NewCanvas("Canvas_End", 0);
        NewFullImage("Background", canvas.transform, spPixel, UiTheme.Ink);
        Text title = NewText("Title", canvas.transform, 0, 16, 320, 26, "DEMO SELESAI", 20, UiTheme.Gold, TextAnchor.MiddleCenter, true, false);
        var outline = title.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color32(74, 36, 12, 255);
        outline.effectDistance = new Vector2(4f, -4f);
        Image partner = NewImage("Partner", canvas.transform, 128, 48, 64, 64, monsters["Pyrit"].Frames[0], Color.white);
        partner.preserveAspect = true;
        Text partnerText = NewText("PartnerText", canvas.transform, 0, 118, 320, 12, "Pyrit · Lv 3", 9, UiTheme.Cream, TextAnchor.MiddleCenter);
        Text playtime = NewText("Playtime", canvas.transform, 0, 132, 320, 10, "Waktu bermain 0 menit 0 detik", 8, UiTheme.Text, TextAnchor.MiddleCenter);
        NewText("Hint", canvas.transform, 0, 154, 320, 10, "Tekan Enter untuk kembali ke judul", 7, UiTheme.Muted, TextAnchor.MiddleCenter);
        NewText("Credit", canvas.transform, 4, 167, 312, 10, $"{StudentName} · NIM {StudentNim}", 7, UiTheme.Muted, TextAnchor.MiddleCenter);
        Button catcher = NewClickCatcher("ClickCatcher", canvas.transform);

        Set(end, "partnerImage", partner);
        Set(end, "partnerText", partnerText);
        Set(end, "timeText", playtime);
        Set(end, "clickCatcher", catcher);
        Set(end, "endMusic", Bgm("BGM_Title"));

        SaveScene(scene, EndScenePath);
    }

    // ---------------------------------------------------------------- P1: scene sandbox pribadi

    static void BuildSandboxScene()
    {
        Scene scene = NewScene();
        NewCamera(null, new Vector3(0f, 0f, -10f), new Color32(38, 43, 56, 255), true);
        NewGlobalLight(1f);

        GameObject player = BuildPlayer(Vector2.zero, out PlayerMovement movement);
        var starter = NewObject("SandboxStarter").AddComponent<SandboxStarter>();
        Set(starter, "player", movement);

        Sprite square = LoadSprite(ArtDir + "/UI/Square.png");
        var wallColor = new Color32(120, 92, 70, 255);
        var obstacleColor = new Color32(90, 120, 150, 255);
        Block("Wall", square, new Vector2(2.5f, 0f), Vector2.one, wallColor);               // P1: Position (2,5; 0)
        Block("Wall_Atas", square, new Vector2(0f, 5f), new Vector2(19f, 1f), wallColor);    // tugas: 4 tembok batas
        Block("Wall_Bawah", square, new Vector2(0f, -5f), new Vector2(19f, 1f), wallColor);
        Block("Wall_Kiri", square, new Vector2(-9f, 0f), new Vector2(1f, 9f), wallColor);
        Block("Wall_Kanan", square, new Vector2(9f, 0f), new Vector2(1f, 9f), wallColor);
        Block("Rintangan_1", square, new Vector2(-4f, 2f), new Vector2(2f, 1f), obstacleColor);   // tugas: 3 rintangan
        Block("Rintangan_2", square, new Vector2(-5f, -2.5f), new Vector2(1f, 2f), obstacleColor);
        Block("Rintangan_3", square, new Vector2(5.5f, -2f), new Vector2(1.5f, 1.5f), obstacleColor);

        // tugas: satu NPC diam (sprite NPC_*) yang tidak bisa ditembus
        SpriteRenderer npc = NewSprite("NPC_Diam", null, new Vector3(4f, 2.5f, 0f),
            Sprites(ArtDir + "/Characters/NPC_Merchant_Walk_16x16.png")[0], "Characters");
        var npcCollider = npc.gameObject.AddComponent<BoxCollider2D>();
        npcCollider.size = new Vector2(0.8f, 0.5f);
        npcCollider.offset = new Vector2(0f, 0.25f);

        // P4: uji MonsterInstance tanpa battle
        var debugger = NewObject("MonsterDebugger").AddComponent<MonsterDebugger>();
        Set(debugger, "species", monsters["Pyrit"]);
        Set(debugger, "level", 4);

        SaveScene(scene, SandboxScenePath);
    }

    static void Block(string name, Sprite square, Vector2 position, Vector2 size, Color color)
    {
        SpriteRenderer sr = NewSprite(name, null, position, square, "Characters");
        sr.color = color;
        sr.transform.localScale = new Vector3(size.x, size.y, 1f);
        sr.gameObject.AddComponent<BoxCollider2D>();
    }

    /// <summary>Player: Rigidbody 2D Dynamic, Gravity 0, Freeze Rotation Z, Interpolate; Box Collider 2D (0.6, 0.4) offset (0, 0.2).</summary>
    static GameObject BuildPlayer(Vector2 position, out PlayerMovement movement)
    {
        GameObject go = NewObject("Player", null, position);
        go.tag = "Player";
        go.layer = LayerMask.NameToLayer("Player");
        Sprite[] sheet = Sprites(ArtDir + "/Characters/Player_Walk_16x16.png");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sheet[0];
        sr.sortingLayerName = "Characters";
        sr.spriteSortPoint = SpriteSortPoint.Pivot;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.6f, 0.4f);
        col.offset = new Vector2(0f, 0.2f);

        movement = go.AddComponent<PlayerMovement>();
        Set(movement, "config", playerConfig);
        Set(movement, "rb", rb);

        var animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = acPlayer;
        var playerAnimator = go.AddComponent<PlayerAnimator>();
        Set(playerAnimator, "movement", movement);
        return go;
    }
}
