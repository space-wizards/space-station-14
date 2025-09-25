using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared.EntityEffects;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class PolymorphEntityEffectSystem : EntityEffectSystem<PolymorphableComponent, Shared.EntityEffects.Effects.Polymorph>
{
    [Dependency] private readonly PolymorphSystem _polymorph = default!;

    protected override void Effect(Entity<PolymorphableComponent> entity, ref EntityEffectEvent<Shared.EntityEffects.Effects.Polymorph> args)
    {
        _polymorph.PolymorphEntity(entity, args.Effect.Prototype);
    }
}
