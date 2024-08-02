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

    [DataField]
    public TimeSpan ThrowCooldown = TimeSpan.FromSeconds(1);

    // Before changing how this is updated, please see SharedPullerSystem.RefreshMovementSpeed
    public float WalkSpeedModifier => Pulling == default ? 1.0f : 0.95f;

    public float SprintSpeedModifier => Pulling == default ? 1.0f : 0.95f;

    /// <summary>
    /// Entity currently being pulled if applicable.
    /// </summary>
    [AutoNetworkedField, DataField]
    public EntityUid? Pulling;

    /// <summary>
    ///     Does this entity need hands to be able to pull something?
    /// </summary>
    [DataField]
    public bool NeedsHands = true;

    [DataField]
    public ProtoId<AlertPrototype> PullingAlert = "Pulling";

    [DataField]
    public Dictionary<GrabStage, short> PullingAlertSeverity = new()
    {
        { GrabStage.No, 0 },
        { GrabStage.Soft, 1 },
        { GrabStage.Hard, 2 },
        { GrabStage.Suffocate, 3 },
    };

    [DataField, AutoNetworkedField]
    public GrabStage GrabStage = GrabStage.No;

    [DataField, AutoNetworkedField]
    public GrubStageDirection GrabStageDirection = GrubStageDirection.Increase;

    [AutoNetworkedField]
    public TimeSpan NextStageChange;

    [DataField]
    public TimeSpan StageChangeCooldown = TimeSpan.FromSeconds(1.5f);

    [DataField]
    public Dictionary<GrabStage, float> EscapeChances = new()
    {
        { GrabStage.No, 1f },
        { GrabStage.Soft, 0.7f },
        { GrabStage.Hard, 0.4f },
        { GrabStage.Suffocate, 0.1f },
    };

    [DataField]
    public float SuffocateGrabStaminaDamage = 10f;

    [DataField]
    public float GrabThrowDamageModifier = 1f;

    [ViewVariables]
    public List<EntityUid> GrabVirtualItems = new();

    [ViewVariables]
    public Dictionary<GrabStage, int> GrabVirtualItemStageCount = new()
    {
        { GrabStage.Suffocate, 1 },
    };
}
