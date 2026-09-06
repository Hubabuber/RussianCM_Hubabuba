using Content.Shared.Administration;
using Content.Shared.Preferences;

namespace Content.Shared._AU14.Administration;

public static class AdminOOCColorResolver
{
    public static Color? Resolve(AdminData? admin, PlayerPreferences? preferences)
    {
        if (admin?.OOCColor is { } groupColor && Color.TryFromHex(groupColor, out var t))
            return t;

        if (admin?.HasFlag(AdminFlags.NameColor) == true)
            return preferences?.AdminOOCColor;

        return null;
    }
}
