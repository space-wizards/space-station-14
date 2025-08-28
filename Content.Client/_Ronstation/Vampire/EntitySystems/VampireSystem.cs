using Content.Shared._Ronstation.Vampire.Components;
using Content.Client.Alerts;
using Content.Shared.Alert;
using Content.Shared.Alert.Components;
using Content.Shared.Antag;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Ronstation.Vampire.EntitySystems;

public sealed class VampireSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireComponent, GetGenericAlertCounterAmountEvent>(OnGetCounterAmount);
    }


    private void OnGetCounterAmount(Entity<VampireComponent> ent, ref GetGenericAlertCounterAmountEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.VitaeAlert != args.Alert)
            return;

        args.Amount = ent.Comp.Vitae.Int();
    }
}