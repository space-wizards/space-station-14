using Content.Shared.Body.Components;
using Content.Shared.Body.Part;

namespace Content.Shared.Humanoid
{
    public static class HumanoidVisualLayersExtension
    {
        public static bool HasSexMorph(HumanoidVisualLayers layer)
        {
            return layer switch
            {
                HumanoidVisualLayers.Chest => true,
                HumanoidVisualLayers.Head => true,
                _ => false
            };
        }

        /// <summary>
        ///     Sublayers. Any other layers that may visually depend on this layer existing.
        ///     For example, the head has layers such as eyes, hair, etc. depending on it.
        /// </summary>
        /// <param name="layer"></param>
        /// <returns>Enumerable of layers that depend on that given layer. Empty, otherwise.</returns>
        /// <remarks>This could eventually be replaced by a body system implementation.</remarks>
        public static IEnumerable<HumanoidVisualLayers> Sublayers(HumanoidVisualLayers layer)
        {
            switch (layer)
            {
                case HumanoidVisualLayers.Head:
                    yield return HumanoidVisualLayers.Head;
                    yield return HumanoidVisualLayers.Eyes;
                    yield return HumanoidVisualLayers.HeadSide;
                    yield return HumanoidVisualLayers.HeadTop;
                    yield return HumanoidVisualLayers.Hair;
                    yield return HumanoidVisualLayers.FacialHair;
                    yield return HumanoidVisualLayers.Snout;
                    break;
                case HumanoidVisualLayers.LArm:
                    yield return HumanoidVisualLayers.LArm;
                    yield return HumanoidVisualLayers.LHand;
                    break;
                case HumanoidVisualLayers.RArm:
                    yield return HumanoidVisualLayers.RArm;
                    yield return HumanoidVisualLayers.RHand;
                    break;
                case HumanoidVisualLayers.LLeg:
                    yield return HumanoidVisualLayers.LLeg;
                    yield return HumanoidVisualLayers.LFoot;
                    break;
                case HumanoidVisualLayers.RLeg:
                    yield return HumanoidVisualLayers.RLeg;
                    yield return HumanoidVisualLayers.RFoot;
                    break;
                case HumanoidVisualLayers.Chest:
                    yield return HumanoidVisualLayers.Chest;
                    yield return HumanoidVisualLayers.Tail;
                    break;
                default:
                    yield break;
            }
        }

        public static (BodyPartType partType, BodyPartSymmetry symmetry) ToBodyPartType(this HumanoidVisualLayers layer)
        {
            switch (layer)
            {
                case HumanoidVisualLayers.Head:
                    return (BodyPartType.Head, BodyPartSymmetry.None);
                case HumanoidVisualLayers.Chest:
                    return (BodyPartType.Torso, BodyPartSymmetry.None);
                case HumanoidVisualLayers.Tail:
                    return (BodyPartType.Tail, BodyPartSymmetry.None);
                case HumanoidVisualLayers.LArm:
                    return (BodyPartType.Arm, BodyPartSymmetry.Left);
                case HumanoidVisualLayers.LHand:
                    return (BodyPartType.Hand, BodyPartSymmetry.Left);
                case HumanoidVisualLayers.RArm:
                    return (BodyPartType.Arm, BodyPartSymmetry.Right);
                case HumanoidVisualLayers.RHand:
                    return (BodyPartType.Hand, BodyPartSymmetry.Right);
                case HumanoidVisualLayers.LLeg:
                    return (BodyPartType.Leg, BodyPartSymmetry.Left);
                case HumanoidVisualLayers.LFoot:
                    return (BodyPartType.Foot, BodyPartSymmetry.Left);
                case HumanoidVisualLayers.RLeg:
                    return (BodyPartType.Leg, BodyPartSymmetry.Right);
                case HumanoidVisualLayers.RFoot:
                    return (BodyPartType.Foot, BodyPartSymmetry.Right);
                default:
                    return (BodyPartType.Other, BodyPartSymmetry.None);
            }
        }

        public static HumanoidVisualLayers? ToHumanoidLayers(this BodyPartSlot part)
        {
            var symmetry = part.Part?.Symmetry ?? BodyPartSymmetry.None;

            switch (part.PartType)
            {
                case BodyPartType.Other:
                    return null;
                case BodyPartType.Torso:
                    return HumanoidVisualLayers.Chest;
                case BodyPartType.Tail:
                    return HumanoidVisualLayers.Tail;
                case BodyPartType.Head:
                    return HumanoidVisualLayers.Head;
                    /* Are you going to hide all of these? Do a call to sublayers instead.
                    return HumanoidVisualLayers.Snout;
                    return HumanoidVisualLayers.HeadSide;
                    return HumanoidVisualLayers.HeadTop;
                    return HumanoidVisualLayers.Eyes;
                    return HumanoidVisualLayers.FacialHair;
                    return HumanoidVisualLayers.Hair;
                    return HumanoidVisualLayers.StencilMask;
                    */
                case BodyPartType.Arm:
                    switch (symmetry)
                    {
                        case BodyPartSymmetry.None:
                            break;
                        case BodyPartSymmetry.Left:
                            return HumanoidVisualLayers.LArm;
                            break;
                        case BodyPartSymmetry.Right:
                            return HumanoidVisualLayers.RArm;
                            break;
                        default:
                            break;
                    }
                    break;
                case BodyPartType.Hand:
                    switch (symmetry)
                    {
                        case BodyPartSymmetry.None:
                            break;
                        case BodyPartSymmetry.Left:
                            return HumanoidVisualLayers.LHand;
                            break;
                        case BodyPartSymmetry.Right:
                            return HumanoidVisualLayers.RHand;
                            break;
                        default:
                            break;
                    }
                    break;
                case BodyPartType.Leg:
                    switch (symmetry)
                    {
                        case BodyPartSymmetry.None:
                            break;
                        case BodyPartSymmetry.Left:
                            return HumanoidVisualLayers.LLeg;
                            break;
                        case BodyPartSymmetry.Right:
                            return HumanoidVisualLayers.RLeg;
                            break;
                        default:
                            break;
                    }
                    break;
                case BodyPartType.Foot:
                    switch (symmetry)
                    {
                        case BodyPartSymmetry.None:
                            break;
                        case BodyPartSymmetry.Left:
                            return HumanoidVisualLayers.LFoot;
                            break;
                        case BodyPartSymmetry.Right:
                            return HumanoidVisualLayers.RFoot;
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }

            return null;
        }
    }
}
