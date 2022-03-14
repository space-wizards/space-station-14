using Robust.Shared.Serialization;

namespace Content.Shared.CharacterAppearance
{
    [Serializable, NetSerializable]
    public enum HumanoidVisualLayers : byte
    {
        TailBehind,
        Hair,
        FacialHair,
        Chest,
        Head,
        Snout,
        HeadSide, // side parts (i.e., frills)
        HeadTop,  // top parts (i.e., ears)
        Eyes,
        RArm,
        LArm,
        RHand,
        LHand,
        RLeg,
        LLeg,
        RFoot,
        LFoot,
        TailFront,
        Handcuffs,
        StencilMask,
        Fire,
    }
}
