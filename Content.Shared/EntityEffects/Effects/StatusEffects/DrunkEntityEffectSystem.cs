using Content.Shared.Drunk;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.StatusEffects;

/// <summary>
/// Applies the drunk status effect to this entity.
/// The duration of the effect is equal to <see cref="Drunk.BoozePower"/> modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class DrunkEntityEffectSystem : EntityEffectSystem<MetaDataComponent, Drunk>
{
    [Dependency] private SharedDrunkSystem _drunk = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, Drunk effect, EntityEffectData data)
    {
        var boozePower = effect.BoozePower * data.Scale;

        _drunk.TryApplyDrunkenness(entity, boozePower);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Drunk : EntityEffect
{
    /// <summary>
    ///     BoozePower is how long each metabolism cycle will make the drunk effect last for.
    /// </summary>
    [DataField]
    public TimeSpan BoozePower = TimeSpan.FromSeconds(3f);

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-drunk", ("chance", Probability));
}
