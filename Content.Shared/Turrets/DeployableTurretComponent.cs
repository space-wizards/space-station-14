using Content.Shared.Damage.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Turrets;

/// <summary>
/// Attached to turrets that can be toggled between an inactive and active state
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
[Access(typeof(SharedDeployableTurretSystem))]
public sealed partial class DeployableTurretComponent : Component
{
    /// <summary>
    /// Whether the turret is toggled 'on' or 'off'
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = false;

    /// <summary>
    /// The current state of the turret. Used to inform the device network. 
    /// </summary>
    [DataField, AutoNetworkedField]
    public DeployableTurretState CurrentState = DeployableTurretState.Retracted;

    /// <summary>
    /// The visual state of the turret. Used on the client-side. 
    /// </summary>
    [DataField]
    public DeployableTurretState VisualState = DeployableTurretState.Retracted;

    /// <summary>
    /// The physics fixture that will have its collisions disabled when the turret is retracted.
    /// </summary>
    [DataField]
    public string? DeployedFixture = "turret";

    /// <summary>
    /// When retracted, the following damage modifier set will be applied to the turret.
    /// </summary>
    [DataField]
    public ProtoId<DamageModifierSetPrototype>? RetractedDamageModifierSetId;

    /// <summary>
    /// When deployed, the following damage modifier set will be applied to the turret.
    /// </summary>
    [DataField]
    public ProtoId<DamageModifierSetPrototype>? DeployedDamageModifierSetId;

    #region: Sound data

    /// <summary>
    /// Sound to play when denied access to the turret.
    /// </summary>
    [DataField]
    public SoundSpecifier AccessDeniedSound = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

    /// <summary>
    /// Sound to play when the turret deploys.
    /// </summary>
    [DataField]
    public SoundSpecifier DeploymentSound = new SoundPathSpecifier("/Audio/Machines/blastdoor.ogg");

    /// <summary>
    /// Sound to play when the turret retracts.
    /// </summary>
    [DataField]
    public SoundSpecifier RetractionSound = new SoundPathSpecifier("/Audio/Machines/blastdoor.ogg");

    #endregion

    #region: Animation data

    /// <summary>
    /// The length of the deployment animation (in seconds)
    /// </summary>
    [DataField]
    public float DeploymentLength = 1.19f;

    /// <summary>
    /// The length of the retraction animation (in seconds)
    /// </summary>
    [DataField]
    public float RetractionLength = 1.19f;

    /// <summary>
    /// The time that the current animation should complete (in seconds)
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan AnimationCompletionTime = TimeSpan.Zero;

    /// <summary>
    /// The animation used when turret activates
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public object DeploymentAnimation = default!;

    /// <summary>
    /// The animation used when turret deactivates
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public object RetractionAnimation = default!;

    /// <summary>
    /// The key used to index the animation played when turning the turret on/off.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public const string AnimationKey = "deployable_turret_animation";

    #endregion

    #region: Visual state data

    /// <summary>
    /// The visual state to use when the turret is deployed.
    /// </summary>
    [DataField]
    public string DeployedState = "cover_open";

    /// <summary>
    /// The visual state to use when the turret is not deployed.
    /// </summary>
    [DataField]
    public string RetractedState = "cover_closed";

    /// <summary>
    /// Used to build the deployment animation when the component is initialized.
    /// </summary>
    [DataField]
    public string DeployingState = "cover_opening";

    /// <summary>
    /// Used to build the retraction animation when the component is initialized.
    /// </summary>
    [DataField]
    public string RetractingState = "cover_closing";

    #endregion
}

[Serializable, NetSerializable]
public enum DeployableTurretVisuals : byte
{
    Turret,
    Weapon,
    Broken,
}

[Serializable, NetSerializable]
public enum DeployableTurretState : byte
{
    Retracted = 0,
    Deployed = (1 << 0),
    Retracting = (1 << 1),
    Deploying = (1 << 1) | Deployed,
    Firing = (1 << 2) | Deployed,
    Disabled = (1 << 3),
    Broken = (1 << 4),
}
