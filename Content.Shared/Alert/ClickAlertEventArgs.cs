using System;
using Robust.Shared.GameObjects;

namespace Content.Shared.Alert;

public class ClickAlertEventArgs : EventArgs
{
    /// <summary>
    /// Player clicking the alert
    /// </summary>
    public readonly EntityUid Player;
    /// <summary>
    /// Alert that was clicked
    /// </summary>
    public readonly AlertPrototype Alert;

    public ClickAlertEventArgs(EntityUid player, AlertPrototype alert)
    {
        Player = player;
        Alert = alert;
    }
}
