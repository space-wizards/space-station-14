using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Wounds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ShockAlertsSystem))]
public sealed partial class ShockAlertsComponent : Component
{
    /// <summary>
    /// The alert to display depending on the amount of shock. Highest key is selected.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, ProtoId<AlertPrototype>> Thresholds;

    /// <summary>
    /// The alert category of the alerts.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AlertCategoryPrototype> AlertCategory;

    /// <summary>
    /// The alert to display when pain is suppressed.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AlertPrototype> SuppressedAlert;

    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype>? CurrentThresholdState;
}
