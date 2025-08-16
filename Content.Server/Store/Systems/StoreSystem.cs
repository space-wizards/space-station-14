using System.Linq;
using Content.Server.Stack;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.Store.Systems;
using Content.Shared.UserInterface;

namespace Content.Server.Store.Systems;

public sealed class StoreSystem : SharedStoreSystem
{
    [Dependency] private readonly StackSystem _stack = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StoreComponent, BeforeActivatableUIOpenEvent>(BeforeActivatableUiOpen);
    }

    private void BeforeActivatableUiOpen(EntityUid uid, StoreComponent component, BeforeActivatableUIOpenEvent args)
    {
        UpdateAvailableListings(args.User, uid, component);
    }

    protected override void WithdrawCurrency(EntityUid user, CurrencyPrototype currency, int amount)
    {
        FixedPoint2 amountRemaining = amount;
        var coordinates = Transform(user).Coordinates;

        if (currency.Cash == null)
            return;

        var sortedCashValues = currency.Cash.Keys.OrderByDescending(x => x).ToList();
        foreach (var value in sortedCashValues)
        {
            var cashId = currency.Cash[value];
            var amountToSpawn = (int) MathF.Floor((float) (amountRemaining / value));
            var stack = _stack.SpawnMultiple(cashId, amountToSpawn, coordinates);
            if (stack.FirstOrDefault() is var stackEnt)
                Hands.PickupOrDrop(user, stackEnt);
            amountRemaining -= value * amountToSpawn;
        }
    }
}
