using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// Perakit demo Pulau Monster (Editor). Membuat ulang aset hasil (tile, rule tile, palette, animasi,
/// data monster, prefab) dan semua scene sesuai PDF P1-P7, lalu bisa membuat build .exe.
/// Menu: Pulau Monster / Rakit Ulang Semua.
/// Command line: Unity.exe -batchmode -projectPath . -executeMethod DemoBuilder.BatchBuildAll
/// </summary>
public static partial class DemoBuilder
{
    public const string Root = "Assets/_Project";
    public const string ArtDir = Root + "/Art";
    public const string DataDir = Root + "/Data";
    public const string PrefabDir = Root + "/Prefabs";
    public const string SceneDir = Root + "/Scenes";
    public const string AnimDir = ArtDir + "/Animations";
    public const string PaletteDir = ArtDir + "/Tiles/Palette";
    public const string AudioDir = Root + "/Audio";

    public const string BootstrapScenePath = SceneDir + "/Scn_Bootstrap.unity";
    public const string TitleScenePath = SceneDir + "/Scn_Title.unity";
    public const string WorldScenePath = SceneDir + "/Scn_World.unity";
    public const string BattleScenePath = SceneDir + "/Scn_Battle.unity";
    public const string EndScenePath = SceneDir + "/Scn_End.unity";
    public const string SandboxScenePath = SceneDir + "/Sandbox/Scn_Reihan.unity";

    public const string StudentName = "M Reihan Fahreza";
    public const string StudentNim = "4352511014";

    [MenuItem("Pulau Monster/Rakit Ulang Semua (aset + scene)", priority = 0)]
    public static void BuildAll()
    {
        DateTime start = DateTime.Now;
        CheckProjectSetup();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CleanGenerated();
        ImportArt();
        CreateTiles();
        CreateAnimations();
        CreateData();
        CreatePrefabs();
        BuildScenes();
        ConfigurePlayerSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);
        Debug.Log($"[DemoBuilder] Selesai dalam {(DateTime.Now - start).TotalSeconds:F1} detik.");
    }

    public static void BatchBuildAll() => RunBatch(BuildAll);

    public static void BatchBuildAllAndPlayer() => RunBatch(() =>
    {
        BuildAll();
        BuildWindowsPlayer();
    });

    public static void BatchBuildPlayer() => RunBatch(BuildWindowsPlayer);

    private static void RunBatch(Action action)
    {
        int code = 0;
        try
        {
            action();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            code = 1;
        }
        EditorApplication.Exit(code);
    }

    // ---------------------------------------------------------------- util

    /// <summary>P2/P3: layer dan sorting layer wajib ada sebelum prefab/scene dirakit.</summary>
    static void CheckProjectSetup()
    {
        foreach (string layer in new[] { "Player", "Interactable", "Wild", "CameraBounds" })
            if (LayerMask.NameToLayer(layer) < 0)
                throw new Exception($"Layer '{layer}' belum ada di Tags and Layers (ProjectSettings/TagManager.asset).");
        foreach (string sorting in new[] { "Ground", "Decor", "Characters" })
            if (!SortingLayer.layers.Any(l => l.name == sorting))
                throw new Exception($"Sorting Layer '{sorting}' belum ada di Tags and Layers.");
    }

    static void CleanGenerated()
    {
        DeleteIfExists(PaletteDir);
        DeleteIfExists(AnimDir);
        DeleteIfExists(DataDir + "/Monsters");
        DeleteIfExists(DataDir + "/Dialogue");
        DeleteIfExists(DataDir + "/Cfg_Player.asset");
        DeleteIfExists(DataDir + "/MonsterDatabase.asset");
        DeleteIfExists(PrefabDir);
        foreach (string scene in new[] { BootstrapScenePath, TitleScenePath, WorldScenePath, BattleScenePath, EndScenePath, SandboxScenePath })
            DeleteIfExists(scene);
        DeleteIfExists(Root + "/Settings/Presets/Pre_PixelSprite.preset");
        AssetDatabase.Refresh();
    }

    static void DeleteIfExists(string path)
    {
        if (AssetDatabase.IsValidFolder(path) || AssetDatabase.LoadMainAssetAtPath(path) != null)
            AssetDatabase.DeleteAsset(path);
    }

    static void EnsureFolder(string path)
    {
        path = path.Replace('\\', '/').TrimEnd('/');
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static T Load<T>(string path) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new Exception($"Aset tidak ditemukan: {path}");
        return asset;
    }

    static Sprite LoadSprite(string path) => Load<Sprite>(path);

    /// <summary>Semua sprite hasil slicing, urut sesuai indeks _0, _1, ...</summary>
    static Sprite[] Sprites(string path)
    {
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderBy(s => SuffixIndex(s.name)).ToArray();
        if (sprites.Length == 0) throw new Exception($"Tidak ada sprite di {path}");
        return sprites;
    }

    static int SuffixIndex(string name)
    {
        int i = name.LastIndexOf('_');
        return i >= 0 && int.TryParse(name.Substring(i + 1), out int n) ? n : 0;
    }

    static AudioClip Sfx(string name) => Load<AudioClip>($"{AudioDir}/SFX/{name}.wav");
    static AudioClip Bgm(string name) => Load<AudioClip>($"{AudioDir}/BGM/{name}.wav");

    static string[] Pngs(string folder) =>
        Directory.GetFiles(folder, "*.png").Select(p => p.Replace('\\', '/')).OrderBy(p => p).ToArray();

    /// <summary>Isi field private [SerializeField] lewat SerializedObject.</summary>
    static void Set(Object target, string field, object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) throw new Exception($"Field '{field}' tidak ada di {target.GetType().Name}");
        Assign(p, value);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Assign(SerializedProperty p, object value)
    {
        switch (value)
        {
            case null: p.objectReferenceValue = null; break;
            case Object o: p.objectReferenceValue = o; break;
            case string s: p.stringValue = s; break;
            case bool b: p.boolValue = b; break;
            case int i: p.intValue = i; break;
            case float f: p.floatValue = f; break;
            case Vector2 v2: p.vector2Value = v2; break;
            case Vector3 v3: p.vector3Value = v3; break;
            case Color c: p.colorValue = c; break;
            case LayerMask mask: p.intValue = mask.value; break;
            case Enum e: p.intValue = Convert.ToInt32(e); break;
            case IList list:
                p.arraySize = list.Count;
                for (int k = 0; k < list.Count; k++) Assign(p.GetArrayElementAtIndex(k), list[k]);
                break;
            default: throw new Exception($"Tipe {value.GetType().Name} belum didukung Set()");
        }
    }

    static Scene NewScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

    static void SaveScene(Scene scene, string path)
    {
        EnsureFolder(Path.GetDirectoryName(path));
        if (!EditorSceneManager.SaveScene(scene, path)) throw new Exception($"Gagal menyimpan scene {path}");
    }

    static GameObject NewObject(string name, Transform parent = null, Vector3? position = null)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        if (position.HasValue) go.transform.localPosition = position.Value;
        return go;
    }

    static Camera NewCamera(Transform parent, Vector3 localPosition, Color background, bool pixelPerfect)
    {
        GameObject go = NewObject("Main Camera", parent, localPosition);
        go.tag = "MainCamera";
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.625f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000f;
        go.AddComponent<UniversalAdditionalCameraData>();
        if (pixelPerfect)
        {
            var ppc = go.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = 16;
            ppc.refResolutionX = 320;
            ppc.refResolutionY = 180;
            ppc.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;
        }
        return cam;
    }

    static Light2D NewGlobalLight(float intensity)
    {
        GameObject go = NewObject("Global Light 2D");
        var light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = intensity;
        light.color = Color.white;
        ApplyToAllSortingLayers(light);
        return light;
    }

    static Light2D NewPointLight(string name, Transform parent, Vector3 localPosition, float inner, float outer, Color color, float intensity)
    {
        GameObject go = NewObject(name, parent, localPosition);
        var light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.pointLightInnerRadius = inner;
        light.pointLightOuterRadius = outer;
        light.color = color;
        light.intensity = intensity;
        ApplyToAllSortingLayers(light);
        return light;
    }

    static void ApplyToAllSortingLayers(Light2D light)
    {
        var so = new SerializedObject(light);
        SerializedProperty p = so.FindProperty("m_ApplyToSortingLayers");
        if (p == null) return;
        int[] ids = SortingLayer.layers.Select(l => l.id).ToArray();
        p.arraySize = ids.Length;
        for (int i = 0; i < ids.Length; i++) p.GetArrayElementAtIndex(i).intValue = ids[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static SpriteRenderer NewSprite(string name, Transform parent, Vector3 localPosition, Sprite sprite, string sortingLayer, int order = 0)
    {
        GameObject go = NewObject(name, parent, localPosition);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = order;
        sr.spriteSortPoint = SpriteSortPoint.Pivot;
        return sr;
    }
}
