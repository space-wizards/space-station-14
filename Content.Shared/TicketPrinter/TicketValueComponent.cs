namespace Content.Shared.TicketPrinter;

[RegisterComponent]
///<summary>
/// Contains the base amount of tickets that will be spawned by <see cref="TicketPrinterComponent"/> when this entity is crafted or reclaimed
///</summary>
public sealed partial class TicketValueComponent : Component
{
    /// <summary>
    /// Base amount of tickets to spawn
    /// </summary>
    [DataField]
    public float TicketValue = 1f;
}
