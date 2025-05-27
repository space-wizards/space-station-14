using Content.Shared.Damage.Systems;
using Robust.Shared.Timing;

namespace Content.Client.Damage.Systems;

public sealed class SoftCritSystem : SharedSoftCritSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float deltaTime)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        base.Update(deltaTime);
    }
}
