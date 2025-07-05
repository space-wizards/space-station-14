using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Den.Vampire.Components;

/// <summary>
/// Component given to entities that quench their thirst for blood by drinking it.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class VampireThirstBloodComponent : Component
{

    /// <summary>
    /// Whether thirst blood should be able to go above the maximum.
    /// </summary>
    [DataField]
    public bool SoftCapMaximum = true;

    /// <summary>
    /// The maximum blood.
    /// Counts as 100% for the alert sprites.
    /// </summary>
    [DataField]
    public float MaxThirstBlood = 300f;

    /// <summary>
    /// The current thirst blood the user has.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CurrentThirstBlood = 300f;

    /// <summary>
    /// Minimum thirst blood, the value cannot go lower than this.
    /// </summary>
    [DataField]
    public float MinThirstBlood;

    /// <summary>
    /// The amount of thirst blood that gets substracted every UpdateInterval.
    /// </summary>
    [DataField]
    public float ThirstBloodDecay = 1.67f; // Blood thirst decreases from full to zero in just over 15 minutes.

    /// <summary>
    /// Previous blood percentage value (0.0 - 1.0), used to calculate the change.
    /// </summary>
    [DataField]
    public float PrevBloodPercentage = 1.0f;


    [DataField]
    public float NeutralBloodPercent = 0.7f;

    /// <summary>
    /// Множитель на скорость голодания от недостатка крови.
    /// </summary>
    [DataField]
    public float HungerRateMultiplier = 4.5f;

    /// <summary>
    /// Множитель на скорость потери жажды от недостатка крови.
    /// </summary>
    [DataField]
    public float ThirstRateMultiplier = 4.0f;

    /// <summary>
    /// The amount of states the thirst blood alert has.
    /// </summary>
    [DataField]
    public int ThirstBloodLayerStates = 16;

    [DataField]
    public ProtoId<AlertPrototype> ThirstBloodAlert = "ThirstBloodAlert";


    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(5);
    public override bool SendOnlyToOwner => true;
}
