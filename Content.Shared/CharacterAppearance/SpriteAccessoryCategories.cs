using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences.Appearance
{
    [Flags]
    [Serializable, NetSerializable]
    public enum SpriteAccessoryCategories
    {
        None = 0,
        HumanHair = 1 << 0,
        HumanFacialHair = 1 << 1,
        VoxHair = 1 << 2,
        VoxFacialHair = 1 << 3
    }
}
