using Content.Shared.Alert;
using Content.Shared.Movement.Pulling.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Movement.Pulling.Components;

/// <summary>
/// Specifies an entity as being able to pull another entity with <see cref="PullableComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(PullingSystem))]
public sealed partial class PullerComponent : Component
{
    // My raiding guild
    /// <summary>
    /// Next time the puller can throw what is being pulled.
    /// Used to avoid spamming it for infinite spin + velocity.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, Access(Other = AccessPermissions.ReadWriteExecute)]
    public TimeSpan NextThrow;

    /// <summary>
    /// Minimum time between pull throws.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ThrowCooldown = TimeSpan.FromSeconds(1);

    // Before changing how this is updated, please see SharedPullerSystem.RefreshMovementSpeed
    public float WalkSpeedModifier => Pulling == default ? 1.0f : 0.95f;

    public float SprintSpeedModifier => Pulling == default ? 1.0f : 0.95f;

    /// <summary>
    /// Entity currently being pulled if applicable.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Pulling;

    /// <summary>
    /// Does this entity need hands to be able to pull something?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool NeedsHands = true;

    /// <summary>
    /// The alert shown to the puller indicating that they are pulling something.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype> PullingAlert = "Pulling";
}

public sealed partial class StopPullingAlertEvent : BaseAlertEvent;
