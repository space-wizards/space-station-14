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

        // We use a seperate component to avoid having to resolve every single satiation type on an entity.
        // Instead, we just have a component that specifies which one we're looking for.
        if (!TryComp<SatiationCounterAlertComponent>(args.SpriteView, out var alert))
            return;

        if (!ProtoMan.Resolve(alert.SatiationType, out var satiation))
            return;

        if (GetValueOrNull(entity, satiation) is not { } amount)
            return;

        args.Amount = (int) amount;
    }
}
