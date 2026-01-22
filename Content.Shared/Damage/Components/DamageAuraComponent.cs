using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Passively damages the entity on a specified interval.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DamageAuraComponent : Component
{

    /// <summary>
    /// The radius of the aura that deals damage.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("malfunctionRadius")]
    public float Radius = 3.5f;

    /// <summary>
    /// Whitelist for entities that can deals damage.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Whitelist for entities that can not deals damage.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Damage / Healing per interval dealt to the entity every interval
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// Alert that a person will receive when they take damage
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<AlertPrototype>? Alert;

    /// <summary>
    /// Delay between damage events in seconds
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Interval = 1f;

    [DataField("nextDamage", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextDamage = TimeSpan.Zero;
}
