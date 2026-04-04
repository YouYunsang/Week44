using UnityEditor;

public class AudioKeyPostprocessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        AudioKeyGenerator.RegenerateIfNeeded(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
    }
}
