using Content.Shared.Alert;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using System.Text.Json.Serialization;

namespace Content.Shared.EntityEffects;

/// <summary>
/// Passively damages the entity on a specified interval.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EntityEffectAuraComponent : Component
{

    /// <summary>
    /// The radius of the aura.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float Radius = 3.5f;

    /// <summary>
    /// Whitelist for entities that can get effects.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Whitelist for entities that can not get effects.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    ///     A list of effects to apply when a player enters the aura.
    /// </summary>
    [DataField]
    public EntityEffect[] Effects = default!;

    /// <summary>
    /// Alert that a person will receive when they get effects
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<AlertPrototype>? Alert;

    /// <summary>
    /// Delay between effects events in seconds
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Interval = 1f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextEntityEffect = TimeSpan.Zero;
}
