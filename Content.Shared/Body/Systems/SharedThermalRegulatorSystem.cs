using Content.Shared.ActionBlocker;
using Content.Shared.Temperature.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Body.Systems;

public abstract class SharedThermalRegulatorSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] protected readonly SharedTemperatureSystem Temperature = default!;
    [Dependency] protected readonly ActionBlockerSystem ActionBlocker = default!;
}
