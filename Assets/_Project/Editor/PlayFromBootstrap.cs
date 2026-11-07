using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Play selalu mulai dari Scn_Bootstrap (P2), scene apa pun yang sedang terbuka.
/// Disimpan di folder Editor (tidak ikut build). [InitializeOnLoad]: jalan otomatis setiap Unity memuat ulang skrip.
/// Pengecualian: scene di folder Sandbox (P1) boleh di-Play langsung.
/// Play tetap dari scene lain? Klik kanan skrip -> Reimport.
/// </summary>
[InitializeOnLoad]
public static class PlayFromBootstrap
{
    const string BootstrapPath = "Assets/_Project/Scenes/Scn_Bootstrap.unity";

    static PlayFromBootstrap()
    {
        EditorApplication.delayCall += Apply;
        EditorSceneManager.activeSceneChangedInEditMode += (previous, next) => Apply();
    }

    static void Apply()
    {
        Scene active = SceneManager.GetActiveScene();
        bool sandbox = active.path.Contains("/Sandbox/");
        EditorSceneManager.playModeStartScene = sandbox ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapPath);
    }
}
