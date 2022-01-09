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
        HeadOrgan, //Ears, Horns
        FaceOrgan, // Muzzle, Snout
        Tail,
        LWing, //Moths, etc
        RWing, //Moths, etc
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
