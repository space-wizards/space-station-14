using Content.Server.Silicons.Laws;
using Content.Shared.EntityEffects;
using Content.Shared.Silicons.Laws.Components;

namespace Content.Server.EntityEffects.Effects.Smite;

/// <summary>
/// Binds this entity to its law provider and notifies it of its laws.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
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

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class SiliconLawBound : EntityEffectBase<SiliconLawBound>;
