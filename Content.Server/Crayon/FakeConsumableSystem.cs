using Content.Server.Administration.Logs;
using Content.Server.Destructible;
using Content.Server.Popups;
using Content.Shared.Crayon;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Throwing;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Server.Crayon;

/// <summary>
/// A system that handles fake consumable logic.
/// </summary>
public sealed partial class FakeConsumableSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private IngestionSystem _ingestion = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private TriggerSystem _trigger = default!;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<FakeConsumableComponent>();
        while (query.MoveNext(out var entity, out var comp))
        {
            comp.LifeSpan -= TimeSpan.FromSeconds(frameTime);
            if (comp.LifeSpan <= TimeSpan.Zero)
            {
                RevealItem(entity, comp, null);
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<FakeConsumableComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var used = args.Used;
        var user = args.User;
        var slot = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.ContainerId);

        if (slot.ContainedEntity != null)
        {
            _popup.PopupCursor(Loc.GetString("fake-consumable-already-contained", ("contained", slot.ContainedEntity), ("owner", ent)), user);
            return;
        }

        if (_whitelist.IsWhitelistPass(ent.Comp.Blacklist, used))
        {
            _popup.PopupCursor(Loc.GetString("fake-consumable-blacklisted-item", ("used", used), ("owner", ent)), user);
            return;
        }

        var doAfterEventArgs = new DoAfterArgs(EntityManager, user, ent.Comp.InsertDelay,
            new FakeConsumableDoAfterEvent(), ent, used: used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };
        _doAfter.TryStartDoAfter(doAfterEventArgs);

        _adminLog.Add(Shared.Database.LogType.InteractUsing, Shared.Database.LogImpact.Medium, $"{ToPrettyString(user):user} inserted {ToPrettyString(used):used} into {ToPrettyString(ent):target}");
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<FakeConsumableComponent> ent, ref FakeConsumableDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used == null)
            return;

        var slot = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.ContainerId);
        _container.Insert(args.Used.Value, slot);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnEaten(Entity<FakeConsumableComponent> ent, ref FullyEatenEvent args)
    {
        var contained = RevealItem(ent, args.User);
        if (!contained.HasValue)
            return;

        _ingestion.TryIngest(args.User, contained.Value);
    }

    [SubscribeLocalEvent]
    private void OnDamageThresholdReached(Entity<FakeConsumableComponent> ent, ref DamageThresholdReached args)
    {
        var contained = RevealItem(ent, null);
        if (!contained.HasValue)
            return;

        var item = contained.Value;
        RaiseLocalEvent(item, args, true);

        if (HasComp<TimerTriggerComponent>(item))
        {
            _trigger.ActivateTimerTrigger(item);
        }
    }

    [SubscribeLocalEvent]
    private void OnLand(Entity<FakeConsumableComponent> ent, ref LandEvent args)
    {
        var contained = RevealItem(ent, null);
        if (!contained.HasValue)
            return;

        var item = contained.Value;
        RaiseLocalEvent(item, ref args, true);

        if (HasComp<TimerTriggerComponent>(item))
        {
            _trigger.ActivateTimerTrigger(item, args.User);
        }
    }

    private bool TryGetContained(EntityUid uid, FakeConsumableComponent comp, out EntityUid? contained)
    {
        contained = null;

        if (_container.TryGetContainer(uid, comp.ContainerId, out var container))
        {
            contained = container.Count == 0 ? null : container.ContainedEntities[0];
            return true;
        }

        return false;
    }

    private EntityUid? RevealItem(EntityUid uid, FakeConsumableComponent comp, EntityUid? user)
    {
        var coords = Transform(uid).Coordinates;

        _audio.PlayPvs(comp.OnVanishSound, coords);
        _popup.PopupCoordinates(Loc.GetString("fake-consumable-vanish", ("owner", uid)), coords);

        if (TryGetContained(uid, comp, out var contained) && contained.HasValue)
        {
            if (!_container.TryGetContainer(uid, comp.ContainerId, out var container))
                return null;

            var item = contained.Value;

            _container.Remove(item, container);
            Del(uid);

            if (user.HasValue)
                _hands.TryPickupAnyHand(user.Value, item);

            return item;
        }

        QueueDel(uid);
        return null;
    }

    private EntityUid? RevealItem(Entity<FakeConsumableComponent> ent, EntityUid? user)
    {
        return RevealItem(ent.Owner, ent.Comp, user);
    }
}
