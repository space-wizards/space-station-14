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
    [DataField, AutoNetworkedField]
    public float SleepTemperature = 280f;

    /// <summary>
    ///     ProtoId of alert that will be used.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> Alert = "ColdBlooded";

    [DataField, AutoNetworkedField]
    public FixedPoint2 ColdCoof = 0;

    [DataField]
    public FixedPoint2 ColdCoofPerSecond = 1;

    [DataField, AutoNetworkedField]
    public FixedPoint2 ColdCoofReqAmount = 100;

    [DataField]
    public LocId PopupId = "reptilian-on-being-cold-popup";

    [ViewVariables]
    public float Accumulator = 0;

    [ViewVariables]
    public bool HasColdTemperature = false;
}
