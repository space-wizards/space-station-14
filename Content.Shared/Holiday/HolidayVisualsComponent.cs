using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Holiday;

/// <summary>
/// This is used for an entity that enables unique visuals on specified holidays.
/// </summary>
/// <remarks> Only one holiday can be celebrated at a time. Conflicting holidays race. </remarks>
[RegisterComponent]
public sealed partial class HolidayVisualsComponent : Component
{
    /// <summary>
    /// A dictionary relating a generic key to a list of holidays.
    /// The key of the first holiday found being celebrated will be set for <see cref="HolidayVisuals"/>.
    /// </summary>
    [DataField]
    public Dictionary<string, List<ProtoId<HolidayPrototype>>> Holidays = new();
}

[Serializable, NetSerializable]
public enum HolidayVisuals : byte
{
    /// <summary>
    /// Stores the key for the current holiday group being celebrated.
    /// If no holiday is celebrated, gets set to <see cref="SharedHolidaySystem.NoHolidayKey"/>
    /// </summary>
    Holiday,
}
