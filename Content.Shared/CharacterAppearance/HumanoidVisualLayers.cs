using System;
using Robust.Shared.Serialization;

namespace Content.Shared.CharacterAppearance
{
    [Serializable, NetSerializable]
    public enum HumanoidVisualLayers
    {
        Hair,
        FacialHair,
        Chest,
        Head,
        Eyes,
        RArm,
        LArm,
        RHand,
        LHand,
        RLeg,
        LLeg,
        RFoot,
        LFoot,
        Handcuffs,
        StencilMask,
        Fire,
    }
}
