﻿using Content.Shared.Drunk;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Body;

public sealed partial class DrunkEntityEffectSystem : EntityEffectSystem<MetaDataComponent, Drunk>
{
    [Dependency] private readonly SharedDrunkSystem _drunk = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<Drunk> args)
    {
        var boozePower = args.Effect.BoozePower * args.Scale;

        _drunk.TryApplyDrunkenness(entity, boozePower);
    }
}

public sealed partial class Drunk : EntityEffectBase<Drunk>
{
    /// <summary>
    ///     BoozePower is how long each metabolism cycle will make the drunk effect last for.
    /// </summary>
    [DataField]
    public TimeSpan BoozePower = TimeSpan.FromSeconds(3f);

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-drunk", ("chance", Probability));
}
