using System;
using Robust.Shared.GameObjects;

namespace Content.Shared.Alert;

public class ClickAlertEventArgs : EventArgs
{
    /// <summary>
    /// Player clicking the alert
    /// </summary>
    public readonly IEntity Player;
    /// <summary>
    /// Alert that was clicked
    /// </summary>
    public readonly AlertPrototype Alert;

    public ClickAlertEventArgs(IEntity player, AlertPrototype alert)
    {
        Player = player;
        Alert = alert;
    }
}