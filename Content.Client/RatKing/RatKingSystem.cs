using Content.Shared.Alert.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.RatKing;

namespace Content.Client.RatKing;

/// <inheritdoc/>
public sealed class RatKingSystem : SharedRatKingSystem
{
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

        if (!TryComp(ent, out HungerComponent? hungerComponent))
            return;

        args.Amount = (int?)hungerComponent.LastAuthoritativeHungerValue;
    }
}
