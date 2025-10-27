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

    protected override void WithdrawCurrency(EntityUid user, CurrencyPrototype currency, int amount)
    {
        FixedPoint2 amountRemaining = msg.Amount;
        var coordinates = Transform(buyer).Coordinates;

        var sortedCashValues = proto.Cash.Keys.OrderByDescending(x => x).ToList();
        foreach (var value in sortedCashValues)
        {
            var cashId = proto.Cash[value];
            var amountToSpawn = (int) MathF.Floor((float) (amountRemaining / value));
            var ents = _stack.SpawnMultipleAtPosition(cashId, amountToSpawn, coordinates);
            if (ents.FirstOrDefault() is {} ent)
                _hands.PickupOrDrop(buyer, ent);
            amountRemaining -= value * amountToSpawn;
        }

        component.Balance[msg.Currency] -= msg.Amount;
        UpdateUserInterface(buyer, uid, component);
    }
}
