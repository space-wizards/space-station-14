using Content.Shared.DoAfter;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Rummaging;

/// <summary>
/// This is used for entities that can rummage for loot.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(RummagingSystem))]
public sealed partial class CanRummageComponent : Component
{
    /// <summary>
    /// A weighted loot table.
    /// Defining this on the rummager so different things can get different stuff out of the same container type.
    /// Can be overridden on the entity with Rummageable. 
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector? RummageLoot;

    /// <summary>
    /// The context menu verb used for the rummage action.
    /// </summary>
    [DataField]
    public LocId RummageVerb = "verb-rummage-text";

    /// <summary>
    /// Rummage speed multiplier.
    /// </summary>
    [DataField]
    public float RummageModifier = 1f;
}

[Serializable, NetSerializable]
public sealed partial class RummageDoAfterEvent : SimpleDoAfterEvent
{

}
