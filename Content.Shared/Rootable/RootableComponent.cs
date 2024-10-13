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
    [DataField]
    public EntProtoId Action = "ActionToggleRootable";

    [DataField]
    public ProtoId<AlertPrototype> RootedAlert = "Rooted";

    [DataField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// Is the entity currently rooted?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Rooted = false;

    /// <summary>
    /// The puddle that is currently affecting this entity.
    /// </summary>
    [DataField]
    public EntityUid? PuddleEntity;

    /// <summary>
    /// The time at which the next absorption metabolism will occur.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextSecond;

    /// <summary>
    /// The max rate at which chemicals are transferred from the puddle to the rooted entity.
    /// </summary>
    [DataField]
    public FixedPoint2 TransferRate = 0.75;

    /// <summary>
    /// The movement speed modifier for when rooting is active.
    /// </summary>
    [DataField]
    public float SpeedModifier = 0.8f;

    /// <summary>
    /// Sound that plays when rooting is toggled.
    /// </summary>
    public SoundSpecifier RootSound = new SoundPathSpecifier("/Audio/Voice/Diona/diona_salute.ogg");
}
