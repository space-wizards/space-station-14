using Content.Shared.Slippery;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Attempts to slip this entity once, using its existing slippery settings when present.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
public sealed partial class SlipEntityEffectSystem : EntityEffectSystem<MetaDataComponent, Slip>
{
    [Dependency] private SlipperySystem _slippery = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<Slip> args)
    {
        var hadSlipComponent = EnsureComp(entity, out SlipperyComponent slipComponent);
        if (!hadSlipComponent)
            slipComponent.SlipData = args.Effect.Slippery;

        _slippery.TrySlip(entity, slipComponent, entity, false);
        if (!hadSlipComponent)
            RemComp(entity, slipComponent);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Slip : EntityEffectBase<Slip>
{
    [DataField]
    public SlipperyEffectEntry Slippery = new();
}
