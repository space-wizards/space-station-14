using Content.Shared.CharacterAppearance;
using Robust.Shared.Serialization;

namespace Content.Shared.Markings
{
    [Serializable, NetSerializable]
    public enum MarkingCategories : byte
    {
        Hair,
        FacialHair,
        Head,
        HeadTop,
        HeadSide,
        Snout,
        Chest,
        Arms,
        Legs,
        Tail,
        Overlay
    }

    public static class MarkingCategoriesConversion
    {
        public static MarkingCategories FromHumanoidVisualLayers(HumanoidVisualLayers layer)
        {
            return layer switch
            {
                HumanoidVisualLayers.Hair => MarkingCategories.Hair,
                HumanoidVisualLayers.FacialHair => MarkingCategories.FacialHair,
                HumanoidVisualLayers.Head => MarkingCategories.Head,
                HumanoidVisualLayers.HeadTop => MarkingCategories.HeadTop,
                HumanoidVisualLayers.HeadSide => MarkingCategories.HeadSide,
                HumanoidVisualLayers.Snout => MarkingCategories.Snout,
                HumanoidVisualLayers.Chest => MarkingCategories.Chest,
                HumanoidVisualLayers.RArm => MarkingCategories.Arms,
                HumanoidVisualLayers.LArm => MarkingCategories.Arms,
                HumanoidVisualLayers.RHand => MarkingCategories.Arms,
                HumanoidVisualLayers.LHand => MarkingCategories.Arms,
                HumanoidVisualLayers.LLeg => MarkingCategories.Legs,
                HumanoidVisualLayers.RLeg => MarkingCategories.Legs,
                HumanoidVisualLayers.LFoot => MarkingCategories.Legs,
                HumanoidVisualLayers.RFoot => MarkingCategories.Legs,
                HumanoidVisualLayers.Tail => MarkingCategories.Tail,
                _ => MarkingCategories.Overlay
            };
        }

        public static bool IsReplaceable(MarkingCategories category)
        {
            return category switch
            {
                MarkingCategories.Hair => true,
                MarkingCategories.FacialHair => true,
                MarkingCategories.HeadTop => true,
                MarkingCategories.HeadSide => true,
                MarkingCategories.Snout => true,
                MarkingCategories.Tail => true,
                _ => false,
            };
        }
    }
}
