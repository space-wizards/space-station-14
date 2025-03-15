using Content.Shared.ActionBlocker;
using Content.Shared.Temperature.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Body.Systems;

public abstract class SharedThermalRegulatorSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming _gameTiming = default!;
    [Dependency] protected readonly SharedTemperatureSystem _tempSys = default!;
    [Dependency] protected readonly ActionBlockerSystem _actionBlockerSys = default!;
}
