using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AudioKeyGenerator
{
    const string GeneratedPath = "Assets/01.Scripts/Generated/AudioKeys.generated.cs";
    static readonly string[] BgmFolders = { "Assets/05.Audio/BGM" };
    static readonly string[] SfxFolders = { "Assets/05.Audio/SFX" };

    [MenuItem("Tools/Audio/Regenerate Audio Keys")]
    public static void Regenerate()
    {
        var bgmKeys = CollectEnumKeys(BgmFolders);
        var sfxKeys = CollectEnumKeys(SfxFolders);

        string content = BuildFileContent(bgmKeys, sfxKeys);
        WriteIfChanged(GeneratedPath, content);
        AssetDatabase.ImportAsset(GeneratedPath, ImportAssetOptions.ForceUpdate);
    }

    public static void RegenerateIfNeeded(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (!HasAudioChange(importedAssets) &&
            !HasAudioChange(deletedAssets) &&
            !HasAudioChange(movedAssets) &&
            !HasAudioChange(movedFromAssetPaths))
        {
            return;
        }

        Regenerate();
    }

    static bool HasAudioChange(IEnumerable<string> paths)
    {
        foreach (string path in paths)
        {
            if (string.IsNullOrEmpty(path)) continue;
            if (!IsUnderAudioFolder(path)) continue;
            if (IsAudioFile(path)) return true;
            if (path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            {
                string withoutMeta = path.Substring(0, path.Length - 5);
                if (IsAudioFile(withoutMeta)) return true;
            }
        }

        return false;
    }

    static bool IsUnderAudioFolder(string assetPath)
    {
        return assetPath.StartsWith("Assets/05.Audio/", StringComparison.OrdinalIgnoreCase);
    }

    static bool IsAudioFile(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return ext == ".wav" || ext == ".mp3" || ext == ".ogg" || ext == ".aiff" || ext == ".aif";
    }

    class AudioKeyEntry
    {
        public string NameKey;
        public string EnumName;
    }

    static List<AudioKeyEntry> CollectEnumKeys(string[] folders)
    {
        var guids = AssetDatabase.FindAssets("t:AudioClip", folders);
        var usedEnumNames = new HashSet<string>(StringComparer.Ordinal);
        var usedNameKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var keys = new List<AudioKeyEntry>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            string nameKey = ToUniqueNameKey(fileName, usedNameKeys);

            string enumName = ToValidIdentifier(nameKey);
            if (string.IsNullOrEmpty(enumName)) enumName = "Clip";

            string unique = enumName;
            int suffix = 2;
            while (!usedEnumNames.Add(unique))
            {
                unique = enumName + suffix;
                suffix++;
            }

            keys.Add(new AudioKeyEntry
            {
                NameKey = nameKey,
                EnumName = unique,
            });
        }

        keys = keys
            .OrderBy(k => k.EnumName, StringComparer.Ordinal)
            .ToList();

        return keys;
    }

    static string ToUniqueNameKey(string source, ISet<string> usedNameKeys)
    {
        string baseKey = string.IsNullOrWhiteSpace(source) ? "Clip" : source.Trim();
        string unique = baseKey;
        int suffix = 2;

        while (!usedNameKeys.Add(unique))
        {
            unique = $"{baseKey}_{suffix}";
            suffix++;
        }

        return unique;
    }

    static string ToValidIdentifier(string source)
    {
        if (string.IsNullOrWhiteSpace(source)) return string.Empty;

        var sb = new StringBuilder(source.Length);
        bool makeUpper = true;

        foreach (char c in source)
        {
            if (char.IsLetterOrDigit(c))
            {
                if (sb.Length == 0 && char.IsDigit(c))
                    sb.Append('_');

                sb.Append(makeUpper ? char.ToUpperInvariant(c) : c);
                makeUpper = false;
            }
            else
            {
                makeUpper = true;
            }
        }

        return sb.ToString();
    }

    static string BuildFileContent(List<AudioKeyEntry> bgmKeys, List<AudioKeyEntry> sfxKeys)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// This file is auto-generated. Do not edit manually.");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        AppendEnum(sb, "AudioBgmKey", bgmKeys);
        sb.AppendLine();
        AppendEnum(sb, "AudioSfxKey", sfxKeys);
        sb.AppendLine();
        AppendLookupClass(sb, bgmKeys, sfxKeys);
        return sb.ToString();
    }

    static void AppendEnum(StringBuilder sb, string enumName, List<AudioKeyEntry> keys)
    {
        sb.AppendLine($"public enum {enumName}");
        sb.AppendLine("{");
        sb.AppendLine("    None = 0,");

        for (int i = 0; i < keys.Count; i++)
            sb.AppendLine($"    {keys[i].EnumName} = {i + 1},");

        sb.AppendLine("}");
    }

    static void AppendLookupClass(StringBuilder sb, List<AudioKeyEntry> bgmKeys, List<AudioKeyEntry> sfxKeys)
    {
        sb.AppendLine("public static class AudioKeyLookup");
        sb.AppendLine("{");
        AppendLookupDictionary(sb, "BgmByName", "AudioBgmKey", bgmKeys);
        sb.AppendLine();
        AppendLookupDictionary(sb, "SfxByName", "AudioSfxKey", sfxKeys);
        sb.AppendLine();
        sb.AppendLine("    public static bool TryGetBgmKey(string nameKey, out AudioBgmKey key)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (string.IsNullOrWhiteSpace(nameKey))");
        sb.AppendLine("        {");
        sb.AppendLine("            key = AudioBgmKey.None;");
        sb.AppendLine("            return false;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        return BgmByName.TryGetValue(nameKey, out key);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public static bool TryGetSfxKey(string nameKey, out AudioSfxKey key)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (string.IsNullOrWhiteSpace(nameKey))");
        sb.AppendLine("        {");
        sb.AppendLine("            key = AudioSfxKey.None;");
        sb.AppendLine("            return false;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        return SfxByName.TryGetValue(nameKey, out key);");
        sb.AppendLine("    }");
        sb.AppendLine("}");
    }

    static void AppendLookupDictionary(StringBuilder sb, string fieldName, string enumName, List<AudioKeyEntry> keys)
    {
        sb.AppendLine($"    public static readonly IReadOnlyDictionary<string, {enumName}> {fieldName} =");
        sb.AppendLine($"        new Dictionary<string, {enumName}>(System.StringComparer.OrdinalIgnoreCase)");
        sb.AppendLine("        {");

        for (int i = 0; i < keys.Count; i++)
        {
            string escapedName = EscapeString(keys[i].NameKey);
            sb.AppendLine($"            [\"{escapedName}\"] = {enumName}.{keys[i].EnumName},");
        }

        sb.AppendLine("        };");
    }

    static string EscapeString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }

    static void WriteIfChanged(string assetPath, string content)
    {
        string fullPath = Path.GetFullPath(assetPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? string.Empty);

        if (File.Exists(fullPath))
        {
            string existing = File.ReadAllText(fullPath);
            if (existing == content) return;
        }

        File.WriteAllText(fullPath, content, Encoding.UTF8);
        Debug.Log("[AudioKeyGenerator] Generated audio key enums.");
    }
}
