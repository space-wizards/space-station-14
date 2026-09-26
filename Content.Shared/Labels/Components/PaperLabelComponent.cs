using Content.Shared.Containers.ItemSlots;
using Content.Shared.Labels.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Labels.Components;

/// <summary>
/// This component allows you to attach and remove a piece of paper to an entity.
/// See LabelStates for the default name of related sprites.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(LabelSystem))]
public sealed partial class PaperLabelComponent : Component
{
    /// <summary>
    /// The slot where the label is stored.
    /// </summary>
    [DataField]
    public ItemSlot LabelSlot = new();
}
