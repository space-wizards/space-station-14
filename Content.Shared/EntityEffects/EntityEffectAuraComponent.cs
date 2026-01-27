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
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class EntityEffectAuraComponent : Component
{

    /// <summary>
    /// The radius of the aura.
    /// </summary>
    [DataField]
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
    [DataField]
    public ProtoId<AlertPrototype>? Alert;

    /// <summary>
    /// Delay between effects events in seconds
    /// </summary>
    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables, AutoPausedField, AutoNetworkedField]
    public TimeSpan NextEntityEffect = TimeSpan.Zero;
}
