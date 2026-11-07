using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Gambar dokumentasi README: render seluruh peta 40 x 36 Scn_World ke Docs/peta.png.</summary>
public static partial class DemoBuilder
{
    [MenuItem("Pulau Monster/Render Peta untuk README (Docs/peta.png)", priority = 40)]
    public static void RenderMapForDocs()
    {
        EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);

        const int scale = 2;   // 2 piksel layar per piksel art
        var rt = new RenderTexture(MapW * 16 * scale, MapH * 16 * scale, 24) { filterMode = FilterMode.Point };
        var go = new GameObject("DocsCamera");
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = MapH / 2f;
        cam.aspect = (float)MapW / MapH;
        cam.transform.position = new Vector3(MapW / 2f, MapH / 2f, -10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.targetTexture = rt;

        var request = new RenderPipeline.StandardRequest { destination = rt };
        if (RenderPipeline.SupportsRenderRequest(cam, request)) RenderPipeline.SubmitRenderRequest(cam, request);
        else cam.Render();

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        texture.Apply();
        RenderTexture.active = previous;

        Directory.CreateDirectory("Docs");
        File.WriteAllBytes("Docs/peta.png", texture.EncodeToPNG());

        cam.targetTexture = null;
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(texture);
        rt.Release();
        Object.DestroyImmediate(rt);
        Debug.Log($"[DemoBuilder] Docs/peta.png dibuat ({MapW * 16 * scale}x{MapH * 16 * scale})");
    }

    public static void BatchRenderDocs() => RunBatch(RenderMapForDocs);

    /// <summary>Samakan sprite awal tiap monster liar di Scn_World dengan spesiesnya (tampilan di Editor).</summary>
    [MenuItem("Pulau Monster/Segarkan Sprite Monster Liar di Scn_World", priority = 41)]
    public static void RefreshWildMonsterSprites()
    {
        var scene = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);
        int count = 0;
        foreach (WildMonster wild in Object.FindObjectsByType<WildMonster>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (wild.Species == null) continue;
            Set(wild.GetComponent<SpriteRenderer>(), "m_Sprite", wild.Species.Frames[0]);
            count++;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[DemoBuilder] Sprite {count} monster liar disegarkan");
    }

    public static void BatchRefreshSpritesDocsAndPlayer() => RunBatch(() =>
    {
        RefreshWildMonsterSprites();
        RenderMapForDocs();
        BuildWindowsPlayer();
    });
}
