using Robust.Shared.Random;

namespace Content.Shared.Delivery;

/// <summary>
/// System responsible for managing multipliers and logic for different delivery modifiers.
/// </summary>
public sealed partial class DeliveryModifierSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeliveryRandomMultiplierComponent, MapInitEvent>(OnRandomMultiplierMapInit);
        SubscribeLocalEvent<DeliveryRandomMultiplierComponent, GetDeliveryMultiplierEvent>(OnGetRandomMultiplier);
    }

    private void OnRandomMultiplierMapInit(Entity<DeliveryRandomMultiplierComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.CurrentMultiplier = _random.NextFloat(ent.Comp.MinMultiplier, ent.Comp.MaxMultiplier);
        Dirty(ent);
    }

    private void OnGetRandomMultiplier(Entity<DeliveryRandomMultiplierComponent> ent, ref GetDeliveryMultiplierEvent args)
    {
        args.Multiplier += ent.Comp.CurrentMultiplier;
    }
}
