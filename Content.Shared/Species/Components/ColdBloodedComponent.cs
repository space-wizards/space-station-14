using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Species.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ColdBloodedComponent : Component
{
    /// <summary>
    ///     Maximum temperature that will be force entity to sleep.
    /// </summary>
    [DataField(required: true)]
    [AutoNetworkedField]
    public float SleepTemperature;

    /// <summary>
    ///     Required SleepCoefficient for forced sleep.
    /// </summary>
    [DataField(required: true)]
    [AutoNetworkedField]
    public FixedPoint2 SleepCoefficientReqAmount;

    /// <summary>
    ///     SleepCoefficient that entity will be get every second.
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 SleepCoefficientPerSecond;

    /// <summary>
    ///     ProtoId of alert that will be used.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> Alert = "ColdBlooded";

    /// <summary>
    ///     LocId for popup.
    /// </summary>
    [DataField]
    public LocId PopupId = "reptilian-on-being-cold-popup";

    /// <summary>
    ///     Bool that will be true, if entity has lower temperature than SleepTemperature.
    /// </summary>
    [ViewVariables]
    public bool HasColdTemperature = false;

    /// <summary>
    ///     Current SleepCoefficient.
    /// </summary>
    [ViewVariables]
    public FixedPoint2 CurrentSleepCoefficient = 0;

    [ViewVariables]
    public float Accumulator = 0;
}
