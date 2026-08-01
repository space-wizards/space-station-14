using Content.Shared.Alert;
using Content.Shared.Alert.Components;
using Content.Shared.Teleportation.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
namespace Content.Shared.Teleportation.Systems;

public sealed partial class AlertTeleportSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlertTeleportComponent, GetGenericAlertCounterAmountEvent>(OnGetCounterAmount);
    }

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
