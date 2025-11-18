using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Containers.ItemSlots;

namespace Content.Server.Nutrition.Components
{
    /// <summary>
    ///     A reusable vessel for smoking
    /// </summary>
    [RegisterComponent, Access(typeof(SmokingSystem))]
    public sealed partial class SmokingPipeComponent : Component
    {
        public const string BowlSlotId = "bowl_slot";

        [DataField("bowl_slot")]
        public ItemSlot BowlSlot = new();
    }
}
