// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Actions;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared.DeadSpace.Renegade.Components;
using Content.Shared.Inventory.Events;
using Robust.Server.Audio;
using Robust.Server.GameObjects;

namespace Content.Server.DeadSpace.Renegade;

public sealed class RenegadeEswordSystem : EntitySystem
{
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RenegadeEswordComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<RenegadeComponent, RecallRenegadeEswordEvent>(OnRecallEsword);
        SubscribeLocalEvent<RenegadeComponent, RenegadeEswordTeleport>(OnTeleportToEsword);
    }

    private void OnEquipped(Entity<RenegadeEswordComponent> ent, ref GotEquippedEvent args)
    {
        if (!ent.Comp.IsConnected)
        {
            ent.Comp.SwordOwner = args.Equipee;
            BindEsword(args.Equipee, ent.Comp);
        }
    }

    private void BindEsword(EntityUid uid, RenegadeEswordComponent comp)
    {
        comp.IsConnected = true;
        _actions.AddAction(uid, "ActionRecallRenegadeEsword");
        _actions.AddAction(uid, "ActionRenegadeEswordTeleport");
    }

    private void OnRecallEsword(EntityUid uid, RenegadeComponent comp, RecallRenegadeEswordEvent args)
    {
        var enumerator = EntityQueryEnumerator<RenegadeEswordComponent>();
        var user = args.Performer;
        while (enumerator.MoveNext(out var swordUid, out var component))
        {
            if (component.SwordOwner == user)
            {
                _hands.TryForcePickupAnyHand(user, swordUid);
                _popup.PopupEntity("Ваш меч телепортируется вам в руки", user, user);
                _audio.PlayPvs(component.TeleportSound, uid);
            }
        }
    }

    private void OnTeleportToEsword(EntityUid uid, RenegadeComponent comp, RenegadeEswordTeleport args)
    {
        var enumerator = EntityQueryEnumerator<RenegadeEswordComponent>();
        while (enumerator.MoveNext(out var swordUid, out var component))
        {
            if (component.SwordOwner == args.Performer)
            {
                var coords = _transform.GetWorldPosition(swordUid);
                _transform.SetWorldPosition(uid, coords);
                _hands.TryForcePickupAnyHand(uid, swordUid);
                _audio.PlayPvs(component.TeleportSound, uid);
            }
        }
    }
}
