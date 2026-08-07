namespace Content.Shared.Doors;

/// <summary>
///     Stateless network protocol for driving a door remotely, e.g. via Airlock controoler
/// </summary>
public static class DoorNetworkCommands
{
    /// <summary>
    ///     Asks for a Status reply. Controllers poll with this while cycling.
    /// </summary>
    public const string Sync = "door_sync";

    public const string Open = "door_open";
    public const string Close = "door_close";
    public const string Bolt = "door_bolt";
    public const string Unbolt = "door_unbolt";

    public const string Status = "door_status";

    public const string ReplyNetId = "door_reply_netid";
    public const string ReplyFrequency = "door_reply_frequency";

    public const string StatusOpen = "door_status_open";

    public const string StatusBolted = "door_status_bolted";

    /// <summary>
    ///     Can this door be bolted at all? (e.g. shutters)
    /// </summary>
    public const string StatusBoltable = "door_status_boltable";
}
