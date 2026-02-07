using Content.Shared.Humanoid.Markings;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid
{
    [Serializable, NetSerializable]
    public enum HumanoidVisualLayers : byte
    {
        Special, // for the cat ears
        Tail,
        TailOverlay, // markings that go ontop of tails
        Hair,
        FacialHair,
        UndergarmentTop,
        UndergarmentBottom,
        Chest,
        Head,
        Snout,
        SnoutCover, // things layered over snouts (i.e. noses)
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
        Overlay,
        Handcuffs,
        StencilMask,
        Ensnare,
        Fire,

    }
}
