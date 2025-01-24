using Content.Shared.Revenant.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Revenant.Components;

/// <summary>
/// This is used for tracking lights that are overloaded
/// and are about to zap a player.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause, Access(typeof(SharedRevenantOverloadedLightsSystem))]
public sealed partial class RevenantOverloadedLightsComponent : Component
{
    /// <summary>
    /// Entity that's about to be zapped.
    /// </summary>
    [DataField]
    public EntityUid? Target;

    /// <summary>
    /// The timer for the zap charging up.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextZapTime;

    /// <summary>
    /// How long until the zap happens.
    /// </summary>
    [DataField]
    public TimeSpan ZapDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// How far the zap can reach.
    /// </summary>
    [DataField]
    public float ZapRange = 4f;

    /// <summary>
    /// The <see cref="EntityPrototype"/> used for the lightning effect.
    /// </summary>
    [DataField]
    public EntProtoId ZapBeamEntityId = "LightningRevenant";

    [ViewVariables]
    public float? OriginalEnergy;

    [ViewVariables]
    public bool OriginalEnabled = false;
}
