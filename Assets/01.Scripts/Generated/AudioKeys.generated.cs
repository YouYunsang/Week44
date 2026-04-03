// This file is auto-generated. Do not edit manually.
using System.Collections.Generic;

public enum AudioBgmKey
{
    None = 0,
    RainEnv = 1,
    TestBGM = 2,
}

public enum AudioSfxKey
{
    None = 0,
    Punch = 1,
    Swing = 2,
    SwordSwing = 3,
}

public static class AudioKeyLookup
{
    public static readonly IReadOnlyDictionary<string, AudioBgmKey> BgmByName =
        new Dictionary<string, AudioBgmKey>(System.StringComparer.OrdinalIgnoreCase)
        {
            ["RainEnv"] = AudioBgmKey.RainEnv,
            ["testBGM"] = AudioBgmKey.TestBGM,
        };

    public static readonly IReadOnlyDictionary<string, AudioSfxKey> SfxByName =
        new Dictionary<string, AudioSfxKey>(System.StringComparer.OrdinalIgnoreCase)
        {
            ["Punch"] = AudioSfxKey.Punch,
            ["Swing"] = AudioSfxKey.Swing,
            ["SwordSwing"] = AudioSfxKey.SwordSwing,
        };

    public static bool TryGetBgmKey(string nameKey, out AudioBgmKey key)
    {
        if (string.IsNullOrWhiteSpace(nameKey))
        {
            key = AudioBgmKey.None;
            return false;
        }

        return BgmByName.TryGetValue(nameKey, out key);
    }

    public static bool TryGetSfxKey(string nameKey, out AudioSfxKey key)
    {
        if (string.IsNullOrWhiteSpace(nameKey))
        {
            key = AudioSfxKey.None;
            return false;
        }

        return SfxByName.TryGetValue(nameKey, out key);
    }
}
