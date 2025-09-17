using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Holiday;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

/// <summary>
/// This handles <see cref="HolidayOnTriggerComponent"/> and changing the holidays.
/// </summary>
public sealed class HolidayTriggerSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedHolidaySystem _holiday = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolidayOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<HolidayOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        _holiday.RefreshCurrentHolidays(ent.Comp.Time);
        _adminLogger.Add(LogType.EventStarted,
            $"{ToPrettyString(args.User):entity} changed the holiday via a trigger on {ToPrettyString(ent.Owner):entity}.");
    }
}
