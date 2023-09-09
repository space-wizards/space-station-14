using Content.Shared.Containers.ItemSlots;

namespace Content.Server.Labels.Components
{
    /// <summary>
    ///     This component allows you to attach and remove a piece of paper to an entity.
    /// </summary>
    [RegisterComponent]
    public sealed partial class PaperLabelComponent : Component
    {
        [DataField("labelSlot")]
        public ItemSlot LabelSlot = new();
    }
}
