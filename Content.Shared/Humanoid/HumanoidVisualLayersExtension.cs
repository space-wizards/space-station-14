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

        public static string GetSexMorph(HumanoidVisualLayers layer, Sex sex, string id)
        {
            if (!HasSexMorph(layer) || sex == Sex.Unsexed)
                return id;

            return $"{id}{sex}";
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
                    yield return HumanoidVisualLayers.SnoutCover;
                    break;
                case HumanoidVisualLayers.Snout:
                    yield return HumanoidVisualLayers.Snout;
                    yield return HumanoidVisualLayers.SnoutCover;
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
    }
}
