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
        ent.Comp.CurrentMultiplierOffset = _random.NextFloat(ent.Comp.MinMultiplierOffset, ent.Comp.MaxMultiplierOffset);
        Dirty(ent);
    }

    private void OnGetRandomMultiplier(Entity<DeliveryRandomMultiplierComponent> ent, ref GetDeliveryMultiplierEvent args)
    {
        args.AdditiveMultiplier += ent.Comp.CurrentMultiplierOffset;
    }
}
