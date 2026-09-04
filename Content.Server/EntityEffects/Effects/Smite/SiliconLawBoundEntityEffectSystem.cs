using Content.Server.Silicons.Laws;
using Content.Shared.EntityEffects;
using Content.Shared.Silicons.Laws.Components;

namespace Content.Server.EntityEffects.Effects.Smite;

public sealed partial class SiliconLawBoundEntityEffectSystem : EntityEffectSystem<SiliconLawProviderComponent, SiliconLawBound>
{
    [Dependency] private SiliconLawSystem _siliconLaws = default!;

    protected override void Effect(Entity<SiliconLawProviderComponent> entity, ref EntityEffectEvent<SiliconLawBound> args)
    {
        EnsureComp<SiliconLawBoundComponent>(entity);
        _siliconLaws.GetLaws(entity.Owner);
        _siliconLaws.NotifyLawsChanged(entity);
    }
}

public sealed partial class SiliconLawBound : EntityEffectBase<SiliconLawBound>;
