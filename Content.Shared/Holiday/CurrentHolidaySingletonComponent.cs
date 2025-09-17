using Content.Shared.CCVar;
using Robust.Shared.Prototypes;

namespace Content.Shared.Holiday;

/// <summary>
/// This is a singleton component, used on a single entity in nullspace to store data for <see cref="SharedHolidaySystem"/>.
/// </summary>
[RegisterComponent, AutoGenerateComponentState]
[Access(typeof(SharedHolidaySystem))]
public sealed partial class CurrentHolidaySingletonComponent : Component
{
    /// <summary>
    /// Set by <see cref="CCVars.HolidaysEnabled"/> to control if holidays should be executed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled;

    /// <summary>
    /// The date used for holidays. Might not be the real world current date if the round passes over midnight, or admin shenanigans.
    /// Set each time the lobby starts.
    /// </summary>
    [DataField]
    public DateTime CurrentDate = DateTime.MinValue;

    /// <summary>
    /// The current holidays being celebrated.
    /// Set each time <see cref="CurrentDate"/> changes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<HolidayPrototype>> CurrentHolidays = new();
}
