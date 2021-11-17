using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Alert;

/// <summary>
///     Handles the icons on the right side of the screen.
///     Should only be used for player-controlled entities.
/// </summary>
[NetworkedComponent]
[ComponentProtoName("Alerts")]
public abstract class SharedAlertsComponent : Component
{
    [ViewVariables] public Dictionary<AlertKey, AlertState> Alerts = new();
}
