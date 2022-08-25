using Content.Shared.Body.Components;
using Content.Shared.Body.Part;

namespace Content.Shared.CharacterAppearance
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
                    break;
                default:
                    yield break;
            }
        }

        public static IEnumerable<HumanoidVisualLayers> ToHumanoidLayers(this SharedBodyPartComponent part)
        {
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
                    yield return HumanoidVisualLayers.Snout;
                    yield return HumanoidVisualLayers.HeadSide;
                    yield return HumanoidVisualLayers.HeadTop;
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
