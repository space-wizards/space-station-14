using Robust.Shared.Prototypes;

namespace Content.Server.Holiday;

/// <summary>
/// This is used for an entity that enables unique visuals on specified holidays.
/// </summary>
[RegisterComponent]
public sealed partial class HolidayVisualsComponent : Component
{
    /// <summary>
    /// A dictionary relating a generic key to a list of holidays.
    /// If any of the holidays are being celebrated, that key will be set for holiday visuals.
    /// </summary>
    [DataField]
    public Dictionary<string, List<ProtoId<HolidayPrototype>>> Holidays = new();
}
