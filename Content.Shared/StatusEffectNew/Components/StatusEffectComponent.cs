using Content.Shared.Alert;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.StatusEffectNew.Components;

/// <summary>
/// Marker component for all status effects - every status effect entity should have it.
/// Provides a link between the effect and the affected entity, and some data common to all status effects.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedStatusEffectsSystem))]
public sealed partial class StatusEffectComponent : Component
{
    /// <summary>
    /// The entity that this status effect is applied to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? AppliedTo;

    /// <summary>
    /// Status effect indication for the player. If Null, no Alert will be displayed. If Null, the effect lasts indefinitely.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype>? Alert;

    /// <summary>
    /// When this effect will end.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan? EndEffectTime;

    /// <summary>
    /// Whitelist, by which it is determined whether this status effect can be imposed on a particular entity.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Blacklist, by which it is determined whether this status effect can be imposed on a particular entity.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Components that will be added to the status effect entity.
    /// </summary>
    /// <remarks>
    /// Important: Only use components that you are sure cannot be added or removed by other systems! At least check that it won't break anything.
    /// </remarks>
    [DataField(serverOnly: true)]
    public ComponentRegistry Components = new();
}
