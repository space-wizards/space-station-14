using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Weather;

public abstract class SharedWeatherSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly IPrototypeManager ProtoMan = default!;

    [Serializable, NetSerializable]
    protected sealed class WeatherComponentState : ComponentState
    {
        public string? Weather;
        public TimeSpan EndTime;
    }
}
