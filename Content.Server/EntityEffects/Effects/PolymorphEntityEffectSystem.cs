using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class PolymorphEntityEffectSystem : EntityEffectSystem<PolymorphableComponent, Polymorph>
{
    [Dependency] private readonly PolymorphSystem _polymorph = default!;

    protected override void Effect(Entity<PolymorphableComponent> entity, ref EntityEffectEvent<Polymorph> args)
    {
        _polymorph.PolymorphEntity(entity, args.Effect.Prototype);
    }
}

public sealed partial class Polymorph : EntityEffectBase<Polymorph>
{
    /// <summary>
    ///     What polymorph prototype is used on effect
    /// </summary>
    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> Prototype;
}
