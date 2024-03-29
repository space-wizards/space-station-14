using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.EntitySystems
{
    public sealed class GasRegenerationSystem : EntitySystem
    {
        [Dependency] private readonly GasTankSystem _gasTank = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<GasRegenerationComponent>();

            while(query.MoveNext(out var uid, out var regen))
            {
                if (_timing.CurTime < regen.NextRegenTime)
                    continue;

                regen.NextRegenTime = _timing.CurTime + regen.Duration;
            }
        }
    }
}
