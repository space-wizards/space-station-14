using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.StatusEffectNew.Components;

/// <summary>
/// Marker component for all status effects - every status effect entity should have it.
/// Provides a link between the effect and the affected entity, and some data common to all status effects.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
[Access(typeof(StatusEffectsSystem))]
[EntityCategory("StatusEffects")]
public sealed partial class StatusEffectComponent : Component
{
    /// <summary>
    /// The entity that this status effect is applied to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? AppliedTo;

    /// <summary>
    /// When this effect will start. Set to Timespan.Zero to start the effect immediately.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan StartEffectTime;

    /// <summary>
    /// When this effect will end. If Null, the effect lasts indefinitely.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan? EndEffectTime;

    /// <summary>
    /// If true, this status effect has been applied. Used to ensure that <see cref="StatusEffectAppliedEvent"/> only fires once.
    /// </summary>
    /// We actually don't want to network this, that way client can apply an effect it's receiving properly!
    [DataField]
    public bool Applied;

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
}
