using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.TicketPrinter;

[RegisterComponent]
///<summary>
/// Spawns bonus entities on creation of entities with a <see cref="TicketValueComponent"/> through crafting or reclaiming
///</summary>
public sealed partial class TicketPrinterComponent : Component
{
    /// <summary>
    /// Entity Prototype to spawn as the "Ticket"
    /// </summary>
    [DataField, ViewVariables]
    public EntProtoId TicketProtoId = "SalvageTicket";

    /// <summary>
    /// How much to multiply the <see cref="TicketValueComponent"/> ticket value by, default 1.
    /// </summary>
    [DataField, ViewVariables]
    public float TicketMultiplier = 1f;

    [DataField, ViewVariables]
    /// <summary>
    /// Whitelist of allowed items that will produce tickets, if no whitelist everything is allowed
    /// </summary>
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// If ticket value ends up less than 1, or has a remainder, store it for the future.
    /// </summary>
    public float Remainder = 0f;
}
