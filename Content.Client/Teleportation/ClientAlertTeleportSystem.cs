using Content.Shared.Alert.Components;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;

namespace Content.Client.Teleportation.Systems;

public sealed partial class ClientAlertTeleportSystem : AlertTeleportSystem
{
    [SubscribeLocalEvent]
    private void OnGetCounterAmount(Entity<AlertTeleportComponent> ent, ref GetGenericAlertCounterAmountEvent args)
    {
        if (args.Handled)
            return;

        if (!ent.Comp.Targets.ContainsKey(args.Alert))
            return;

        if (ent.Comp.Targets[args.Alert].Targets == null)
            return;

        args.Amount = ent.Comp.Targets[args.Alert].Targets.Count;
    }
}
