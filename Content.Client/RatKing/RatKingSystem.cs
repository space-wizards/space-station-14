using Content.Shared.Alert.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.RatKing;

namespace Content.Client.RatKing;

/// <inheritdoc/>
public sealed class RatKingSystem : SharedRatKingSystem
{
    [Dependency] private readonly HungerSystem _hunger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RatKingComponent, GetGenericAlertCounterAmountEvent>(OnGetCounterAmount);
    }

    private void OnGetCounterAmount(Entity<RatKingComponent> ent, ref GetGenericAlertCounterAmountEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.HungerAlertCategory != args.Alert.Category)
            return;

        if (!TryComp<HungerComponent>(ent, out var hungerComponent))
            return;

        args.Amount = (int?)_hunger.GetHunger(hungerComponent);
    }
}
