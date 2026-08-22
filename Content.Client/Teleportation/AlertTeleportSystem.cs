using Content.Shared.Alert.Components;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;

namespace Content.Client.Teleportation;

public sealed partial class AlertTeleportSystem : SharedAlertTeleportSystem
{
    [SubscribeLocalEvent]
    private void OnGetCounterAmount(Entity<AlertTeleportComponent> ent, ref GetGenericAlertCounterAmountEvent args)
    {
        if (args.Handled)
            return;

        if (!ent.Comp.Targets.TryGetValue(args.Alert, out var target))
            return;

        if (target.Targets.Count == 0)
            return;

        args.Amount = target.Targets.Count;
    }
}
