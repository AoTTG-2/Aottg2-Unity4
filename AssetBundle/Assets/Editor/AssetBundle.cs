// C# Example
// Builds an asset bundle from the selected objects in the project view.
// Once compiled go to "Menu" -> "Assets" and select one of the choices
// to build the Asset Bundle

using UnityEngine;
using UnityEditor;

public class ExportAssetBundles
{
    [MenuItem("Assets/Build AssetBundle From Selection - Track dependencies")]
    static void ExportResource()
    {
        string path = EditorUtility.SaveFilePanel("Save Resource", "", "New Resource", "unity3d");
        if (path.Length != 0)
        {
            // Build the resource file from the active selection.
            Object[] selection = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);
            BuildPipeline.BuildAssetBundle(Selection.activeObject, selection, path,
                              BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets | BuildAssetBundleOptions.UncompressedAssetBundle);
            Selection.objects = selection;
        }
    }
}
