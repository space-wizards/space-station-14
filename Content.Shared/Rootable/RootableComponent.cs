using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Rootable;

/// <summary>
/// A rooting action, for Diona.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class RootableComponent : Component
{
    /// <summary>
    /// The action prototype that toggles the rootable state.
    /// </summary>
    [DataField]
    public EntProtoId Action = "ActionToggleRootable";

    /// <summary>
    /// Entity to hold the action prototype.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// The prototype for the "rooted" alert, indicating the user that they are rooted.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> RootedAlert = "Rooted";

    /// <summary>
    /// Is the entity currently rooted?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Rooted;

    /// <summary>
    /// The puddle that is currently affecting this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? PuddleEntity;

    /// <summary>
    /// The time at which the next absorption metabolism will occur.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextUpdate;

    /// <summary>
    /// The max rate (in reagent units per transfer) at which chemicals are transferred from the puddle to the rooted entity.
    /// </summary>
    [DataField]
    public FixedPoint2 TransferRate = 0.75;

    /// <summary>
    /// The frequency of which chemicals are transferred from the puddle to the rooted entity.
    /// </summary>
    [DataField]
    public TimeSpan TransferFrequency = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The movement speed modifier for when rooting is active.
    /// </summary>
    [DataField]
    public float SpeedModifier = 0.8f;

    /// <summary>
    /// Sound that plays when rooting is toggled.
    /// </summary>
    [DataField]
    public SoundSpecifier RootSound = new SoundPathSpecifier("/Audio/Voice/Diona/diona_salute.ogg");
}
