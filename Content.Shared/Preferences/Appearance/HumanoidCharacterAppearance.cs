using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences.Appearance
{
    [Serializable, NetSerializable]
    public enum CharacterVisuals
    {
        HairStyle,
        HairColor,
        FacialHairStyle,
        FacialHairColor
    }

    public enum HumanoidVisualLayers
    {
        Hair,
        FacialHair,
    }
}
