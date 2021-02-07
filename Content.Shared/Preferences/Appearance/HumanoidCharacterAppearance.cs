using System;
using Content.Shared.GameObjects.Components.Body.Part;
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

    public static class HumanoidVisualLayersExtension
    {
        public static HumanoidVisualLayers? ToHumanoidLayer(this IBodyPart part)
        {
            return part.PartType switch
            {
                BodyPartType.Other => null,
                BodyPartType.Torso => HumanoidVisualLayers.Chest,
                BodyPartType.Head => HumanoidVisualLayers.Head,
                BodyPartType.Arm => part.Symmetry switch
                {
                    BodyPartSymmetry.None => null,
                    BodyPartSymmetry.Left => HumanoidVisualLayers.LArm,
                    BodyPartSymmetry.Right => HumanoidVisualLayers.RArm,
                    _ => throw new ArgumentOutOfRangeException()
                },
                BodyPartType.Hand => part.Symmetry switch
                {
                    BodyPartSymmetry.None => null,
                    BodyPartSymmetry.Left => HumanoidVisualLayers.LHand,
                    BodyPartSymmetry.Right => HumanoidVisualLayers.RHand,
                    _ => throw new ArgumentOutOfRangeException()
                },
                BodyPartType.Leg => part.Symmetry switch
                {
                    BodyPartSymmetry.None => null,
                    BodyPartSymmetry.Left => HumanoidVisualLayers.LLeg,
                    BodyPartSymmetry.Right => HumanoidVisualLayers.RLeg,
                    _ => throw new ArgumentOutOfRangeException()
                },
                BodyPartType.Foot => part.Symmetry switch
                {
                    BodyPartSymmetry.None => null,
                    BodyPartSymmetry.Left => HumanoidVisualLayers.LFoot,
                    BodyPartSymmetry.Right => HumanoidVisualLayers.RFoot,
                    _ => throw new ArgumentOutOfRangeException()
                },
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
