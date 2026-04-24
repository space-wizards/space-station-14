using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Organs;

[RegisterComponent, NetworkedComponent]
[Access(typeof(OxygenAlertsOrganSystem))]
public sealed partial class OxygenAlertsOrganComponent : Component
{
    /// <summary>
    /// The alert used to display oxygen level.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AlertPrototype> Alert;

    /// <summary>
    /// The alert category of the oxygen alerts.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AlertCategoryPrototype> AlertCategory;
}
