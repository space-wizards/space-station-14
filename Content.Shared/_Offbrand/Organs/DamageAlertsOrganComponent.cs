using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Organs;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(DamageAlertsOrganSystem))]
public sealed partial class DamageAlertsOrganComponent : Component
{
    /// <summary>
    /// The alert to display depending on the amount of organ damage. Highest key is selected.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, ProtoId<AlertPrototype>> AlertThresholds;

    /// <summary>
    /// The alert category of the alerts.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AlertCategoryPrototype> AlertCategory;

    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype>? CurrentAlertThresholdState;
}
