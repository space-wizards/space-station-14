using System.Collections.Generic;
using Content.Client.Alerts.UI;
using Content.Shared.Alert;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Client.Alerts;

/// <inheritdoc />
[RegisterComponent]
[ComponentReference(typeof(SharedAlertsComponent))]
public sealed class ClientAlertsComponent : SharedAlertsComponent
{
    [ViewVariables] public readonly Dictionary<AlertKey, AlertControl> AlertControls = new();
    public AlertOrderPrototype? AlertOrder;
    public AlertsUI? AlertUi;
}
