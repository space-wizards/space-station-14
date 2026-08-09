using Content.Shared.Alert.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Client.Nutrition.EntitySystems;

/// <inheritdoc/>
public sealed partial class ClientSatiationSystem : SatiationSystem
{
    [SubscribeLocalEvent]
    private void OnGenericCounter(Entity<SatiationComponent> entity, ref GetGenericAlertCounterAmountEvent args)
    {
        if (args.Handled)
            return;

        var values = entity.Comp.Satiations.Values;

        foreach (var value in values)
        {
            if (!ProtoMan.Resolve(value.Prototype, out var satiation))
                continue;

            if (!satiation.Alerts.ContainsValue(args.Alert))
                continue;

            if (GetValueOrNull(entity, value.SatiationType) is not { } amount)
                continue;

            args.Amount = (int) amount;
        }
    }
}
