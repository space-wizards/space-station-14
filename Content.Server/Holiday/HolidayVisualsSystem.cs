using System.Linq;
using Content.Server.GameTicking;
using Content.Shared.Holiday;

namespace Content.Server.Holiday;

public sealed class HolidayVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly HolidaySystem _holiday = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<HolidayVisualsComponent, ComponentInit>(OnVisualsInit);
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
    }
    
    private void OnVisualsInit(Entity<HolidayVisualsComponent> ent, ref ComponentInit args)
    {
        foreach (var (key, holidays) in ent.Comp.Holidays)
        {
            if (!holidays.Any(h => _holiday.IsCurrentlyHoliday(h)))
                continue;
            _appearance.SetData(ent, HolidayVisuals.Holiday, key);
            break;
        }
    }
    
    private void OnRunLevelChanged(GameRunLevelChangedEvent eventArgs)
    {
        if (!_holiday.enabled) return;

        switch (eventArgs.New)
        {
        case GameRunLevel.PreRoundLobby:
            _holiday.RefreshCurrentHolidays();
            break;
        case GameRunLevel.InRound:
            _holiday.DoGreet();
            _holiday.DoCelebrate();
            break;
        case GameRunLevel.PostRound:
            break;
        }
    }
}