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

        public static IEnumerable<HumanoidVisualLayers> ToHumanoidLayers(this BodyPartSlot part)
        {
            var symmetry = part.Part?.Symmetry ?? BodyPartSymmetry.None;

            switch (part.PartType)
            {
                case BodyPartType.Other:
                    yield break;
                case BodyPartType.Torso:
                    yield return HumanoidVisualLayers.Chest;
                    break;
                case BodyPartType.Tail:
                    yield return HumanoidVisualLayers.Tail;
                    break;
                case BodyPartType.Head:
                    yield return HumanoidVisualLayers.Head;
                    /* Are you going to hide all of these? Do a call to sublayers instead.
                    yield return HumanoidVisualLayers.Snout;
                    yield return HumanoidVisualLayers.HeadSide;
                    yield return HumanoidVisualLayers.HeadTop;
                    yield return HumanoidVisualLayers.Eyes;
                    yield return HumanoidVisualLayers.FacialHair;
                    yield return HumanoidVisualLayers.Hair;
                    yield return HumanoidVisualLayers.StencilMask;
                    */
                    break;
                case BodyPartType.Arm:
                    switch (symmetry)
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
                    switch (symmetry)
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
                    switch (symmetry)
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
                    switch (symmetry)
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
