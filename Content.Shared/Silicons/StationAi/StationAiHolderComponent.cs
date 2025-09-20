using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// Allows moving a <see cref="StationAiCoreComponent"/> contained entity to and from this component.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StationAiHolderComponent : Component
{
    public const string Container = StationAiCoreComponent.Container;

    /// <summary>
    /// Whether the holder should be renamed to the name of the inserted object.
    /// </summary>
    [DataField]
    public bool RenameOnInsert = true;

    [DataField]
    public ItemSlot Slot = new();
}
