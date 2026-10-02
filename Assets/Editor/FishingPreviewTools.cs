#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FishingPreviewTools
{
    private const string FishingScenePath = "Assets/Scenes/FishingDemo.unity";

    [MenuItem("Tools/Fishing/Refresh Art Preview")]
    public static void RefreshArtPreview()
    {
        var scene = EditorSceneManager.OpenScene(FishingScenePath, OpenSceneMode.Single);
        foreach (var builder in Object.FindObjectsByType<PixelMapBuilder>(FindObjectsSortMode.None))
            builder.EnsureDecorations();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }
}
#endif
