using Content.Server.DoAfter;
using Content.Server.Kitchen.Components;
using Content.Server.Popups;
using Content.Shared.Body.Components;
using Content.Shared.Interaction;
using Content.Shared.MobState.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server.Kitchen.EntitySystems;

public sealed class SharpSystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharpComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SharpButcherDoafterComplete>(OnDoafterComplete);
        SubscribeLocalEvent<SharpButcherDoafterCancelled>(OnDoafterCancelled);
    }

    private void OnAfterInteract(EntityUid uid, SharpComponent component, AfterInteractEvent args)
    {
        if (args.Target is null || !TryComp<SharedButcherableComponent>(args.Target, out var butcher))
            return;

        if (butcher.Type != ButcheringType.Knife)
            return;

        if (TryComp<MobStateComponent>(args.Target, out var mobState) && !mobState.IsDead())
            return;

        if (!component.Butchering.Add(args.Target.Value))
            return;

        var doAfter =
            new DoAfterEventArgs(args.User, component.ButcherDelayModifier * butcher.ButcherDelay, default, args.Target)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                NeedHand = true,
                BroadcastFinishedEvent = new SharpButcherDoafterComplete { User = args.User, Entity = args.Target.Value, Sharp = uid },
                BroadcastCancelledEvent = new SharpButcherDoafterCancelled { Entity = args.Target.Value, Sharp = uid }
            };

        _doAfterSystem.DoAfter(doAfter);
    }

    private void OnDoafterComplete(SharpButcherDoafterComplete ev)
    {
        if (!TryComp<SharedButcherableComponent>(ev.Entity, out var butcher))
            return;

        if (!TryComp<SharpComponent>(ev.Sharp, out var sharp))
            return;

        sharp.Butchering.Remove(ev.Entity);

        EntityUid popupEnt = default;
        for (int i = 0; i < butcher.Pieces; i++)
        {
            popupEnt = Spawn(butcher.SpawnedPrototype, Transform(ev.Entity).Coordinates);
        }

        _popupSystem.PopupEntity(Loc.GetString("butcherable-knife-butchered-success", ("target", ev.Entity), ("knife", ev.Sharp)),
            popupEnt, Filter.Entities(ev.User));

        if (TryComp<SharedBodyComponent>(ev.Entity, out var body))
        {
            body.Gib();
        }
        else
        {
            QueueDel(ev.Entity);
        }
    }

    private void OnDoafterCancelled(SharpButcherDoafterCancelled ev)
    {
        if (!TryComp<SharpComponent>(ev.Sharp, out var sharp))
            return;

        sharp.Butchering.Remove(ev.Entity);
    }
}

public sealed class SharpButcherDoafterComplete : EntityEventArgs
{
    public EntityUid Entity;
    public EntityUid Sharp;
    public EntityUid User;
}

public sealed class SharpButcherDoafterCancelled : EntityEventArgs
{
    public EntityUid Entity;
    public EntityUid Sharp;
}
