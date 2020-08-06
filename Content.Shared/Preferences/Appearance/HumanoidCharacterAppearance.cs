using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences.Appearance
{
    [Serializable, NetSerializable]
    public enum HumanoidVisualLayers
    {
        Hair,
        FacialHair,
        Chest,
        Head,
        RArm,
        LArm,
        RHand,
        LHand,
        RLeg,
        LLeg,
        RFoot,
        LFoot,
        StencilMask
    }
}
