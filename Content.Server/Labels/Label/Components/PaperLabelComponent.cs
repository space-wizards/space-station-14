using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Labels.Components
{
    /// <summary>
    ///     This component allows you to attach and remove a piece of paper to an entity.
    /// </summary>
    [RegisterComponent]
    public class PaperLabelComponent : Component
    {
        [DataField("labelSlot")]
        public ItemSlot LabelSlot = new();
    }
}
