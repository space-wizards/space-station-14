namespace Content.Shared.EntityEffects.Effects.Body;

public sealed partial class VomitEntityEffectSystem : EntityEffectSystem<MetaDataComponent, Vomit>
{
    //[Dependency] private readonly VomitSystem _vomit = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<Vomit> args)
    {
        if (args.Scale < 1f)
            return;

        // TODO: Need vomiting in shared...
        //_vomit.Vomit(entity.Owner, args.Effect.ThirstAmount, args.Effect.HungerAmount);
    }
}

public sealed class Vomit : EntityEffectBase<Vomit>
{
    /// How many units of thirst to add each time we vomit
    [DataField]
    public float ThirstAmount = -8f;
    /// How many units of hunger to add each time we vomit
    [DataField]
    public float HungerAmount = -8f;
}
