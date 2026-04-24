using Content.Shared._Offbrand.Wounds;
using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.EntityEffects;

public sealed partial class StartHeart : EntityEffectBase<StartHeart>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("entity-effect-guidebook-start-heart", ("chance", Probability));
    }
}

public sealed class StartHeartEntityEffectSystem : EntityEffectSystem<BodyComponent, StartHeart>
{
    // [Dependency] private readonly HeartSystem _heart = default!;

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<StartHeart> args)
    {
        throw new NotImplementedException("TODO"); // TODO
        // _heart.TryRestartHeart(ent.AsNullable());
    }
}
