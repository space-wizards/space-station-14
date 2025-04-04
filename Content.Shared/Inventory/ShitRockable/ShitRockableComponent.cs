using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Robust.Shared.Serialization;

namespace Content.Shared.Inventory.ShitRockable.Components;

/// <summary>
/// Placed on an entity which is the target of attacks such that its shit may be rocked.
/// </summary>
[RegisterComponent]
public sealed partial class ShitRockableComponent : Component
{
    /// <summary>
    /// list of the inventory slot as slotflag and the damage specifier as threshold for having your shit rocked.
    /// </summary>
    [DataField]
    public List<SlotDamageThreshold> SlotThresholds = [];
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class SlotDamageThreshold
{
    [DataField(required: true)]
    public SlotFlags Slots = SlotFlags.NONE;

    [DataField(required: true)]
    public DamageSpecifier DamageThreshold = default!;
}
