using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Body;

public sealed partial class VomitEntityEffectSystem : EntityEffectSystem<MetaDataComponent, Vomit>
{
    //[Dependency] private readonly VomitSystem _vomit = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<Vomit> args)
    {
        // TODO: Need vomiting in shared...
        //_vomit.Vomit(entity.Owner, args.Effect.ThirstAmount * args.Scale, args.Effect.HungerAmount * args.Scale);
    }
}

public sealed partial class Vomit : EntityEffectBase<Vomit>
{
    /// How many units of thirst to add each time we vomit
    [DataField]
    public float ThirstAmount = -8f;

    /// How many units of hunger to add each time we vomit
    [DataField]
    public float HungerAmount = -8f;

    protected override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-vomit", ("chance", Probability));
}
