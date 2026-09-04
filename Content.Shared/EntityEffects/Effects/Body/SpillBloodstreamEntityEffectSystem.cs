using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class SpillBloodstreamEntityEffectSystem : EntityEffectSystem<BloodstreamComponent, SpillBloodstream>
{
    [Dependency] private BloodstreamSystem _bloodstream = default!;

    protected override void Effect(Entity<BloodstreamComponent> entity, ref EntityEffectEvent<SpillBloodstream> args)
    {
        _bloodstream.SpillAllSolutions(entity.AsNullable());
    }
}

public sealed partial class SpillBloodstream : EntityEffectBase<SpillBloodstream>;
