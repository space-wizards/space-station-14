using System;
using System.Collections.Generic;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;

namespace Content.Shared.CharacterAppearance
{
    public static class HumanoidVisualLayersExtension
    {
        public static IEnumerable<HumanoidVisualLayers> ToHumanoidLayers(this SharedBodyPartComponent part)
        {
            switch (part.PartType)
            {
                case BodyPartType.Other:
                    yield break;
                case BodyPartType.Torso:
                    yield return HumanoidVisualLayers.Chest;
                    break;
                case BodyPartType.Head:
                    yield return HumanoidVisualLayers.Head;
                    yield return HumanoidVisualLayers.Eyes;
                    yield return HumanoidVisualLayers.FacialHair;
                    yield return HumanoidVisualLayers.Hair;
                    yield return HumanoidVisualLayers.StencilMask;
                    break;
                case BodyPartType.Arm:
                    switch (part.Symmetry)
                    {
                        case BodyPartSymmetry.None:
                            yield break;
                        case BodyPartSymmetry.Left:
                            yield return HumanoidVisualLayers.LArm;
                            break;
                        case BodyPartSymmetry.Right:
                            yield return HumanoidVisualLayers.RArm;
                            break;
                        default:
                            yield break;
                    }
                    yield break;
                case BodyPartType.Hand:
                    switch (part.Symmetry)
                    {
                        case BodyPartSymmetry.None:
                            yield break;
                        case BodyPartSymmetry.Left:
                            yield return HumanoidVisualLayers.LHand;
                            break;
                        case BodyPartSymmetry.Right:
                            yield return HumanoidVisualLayers.RHand;
                            break;
                        default:
                            yield break;
                    }
                    yield break;
                case BodyPartType.Leg:
                    switch (part.Symmetry)
                    {
                        case BodyPartSymmetry.None:
                            yield break;
                        case BodyPartSymmetry.Left:
                            yield return HumanoidVisualLayers.LLeg;
                            break;
                        case BodyPartSymmetry.Right:
                            yield return HumanoidVisualLayers.RLeg;
                            break;
                        default:
                            yield break;
                    }
                    yield break;
                case BodyPartType.Foot:
                    switch (part.Symmetry)
                    {
                        case BodyPartSymmetry.None:
                            yield break;
                        case BodyPartSymmetry.Left:
                            yield return HumanoidVisualLayers.LFoot;
                            break;
                        case BodyPartSymmetry.Right:
                            yield return HumanoidVisualLayers.RFoot;
                            break;
                        default:
                            yield break;
                    }
                    yield break;
                default:
                    yield break;
            }
        }
    }
}
