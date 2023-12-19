using Content.Shared.Light.Components;
using Robust.Shared.Timing;

namespace Content.Server.Light
{
    public sealed partial class LightCycleSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<LightCycleComponent, ComponentStartup>(OnComponentStartup);
        }

        private void OnComponentStartup(EntityUid uid, LightCycleComponent cycle, ComponentStartup args)
        {
            cycle.Offset = _gameTiming.RealTime.TotalSeconds;
        }

    }

}
