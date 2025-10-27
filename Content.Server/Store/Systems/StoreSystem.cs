using System.Linq;
using Content.Server.Stack;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Content.Shared.Store.Systems;

namespace Content.Server.Store.Systems;

public sealed class StoreSystem : SharedStoreSystem
{
    [Dependency] private readonly StackSystem _stack = default!;

    protected override void WithdrawCurrency(EntityUid buyer, CurrencyPrototype proto, FixedPoint2 amount)
    {
        // we need an actually valid entity to spawn. This check has been done earlier, but just in case.
        if (proto.Cash == null || !proto.CanWithdraw)
            return;

        FixedPoint2 amountRemaining = amount;
        var coordinates = Transform(buyer).Coordinates;

        var sortedCashValues = proto.Cash.Keys.OrderByDescending(x => x).ToList();
        foreach (var value in sortedCashValues)
        {
            var cashId = proto.Cash[value];
            var amountToSpawn = (int) MathF.Floor((float) (amountRemaining / value));
            var ents = _stack.SpawnMultipleAtPosition(cashId, amountToSpawn, coordinates);
            if (ents.FirstOrDefault() is var ent)
                Hands.PickupOrDrop(buyer, ent);
            amountRemaining -= value * amountToSpawn;
        }
    }
}
