using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Storage.Components;
using Content.Shared.Verbs;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Storage.EntitySystems;

/// <summary>
/// <see cref="MagnetPickupComponent"/>
/// </summary>
public sealed partial class MagnetPickupSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedStorageSystem _storage = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;

    [Dependency] private EntityQuery<PhysicsComponent> _physicsQuery = default!;

    private static readonly TimeSpan ScanDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Reused list of nearby pickup candidates so we can sort them deterministically without allocating every scan.
    /// </summary>
    private readonly List<EntityUid> _nearby = [];


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MagnetPickupComponent, MapInitEvent>(OnMagnetMapInit);
        SubscribeLocalEvent<MagnetPickupComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<MagnetPickupComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<MagnetPickupComponent, MagnetPickupDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<MagnetPickupComponent, DoAfterAttemptEvent<MagnetPickupDoAfterEvent>>(OnDoAfterAttempt);
    }

    private void OnMagnetMapInit(EntityUid uid, MagnetPickupComponent component, MapInitEvent args)
    {
        component.NextScan = _timing.CurTime;
    }

    private void OnExamined(Entity<MagnetPickupComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.ManualTriggerOnly && !ent.Comp.Enabled)
        {
            args.PushMarkup(Loc.GetString("magnet-pickup-examine-off"));
            return;
        }

        if (!CanSiphon(ent))
        {
            args.PushMarkup(Loc.GetString("magnet-pickup-examine-unavailable"));
            return;
        }

        args.PushMarkup(Loc.GetString(ent.Comp.ManualTriggerOnly ? "magnet-pickup-examine-manual" : "magnet-pickup-examine-on"));
    }

    private void OnGetVerbs(Entity<MagnetPickupComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (ent.Comp.ManualTriggerOnly)
        {
            if (!CanSiphon(ent))
                return;

            var user = args.User;
            args.Verbs.Add(new Verb
            {
                Text = Loc.GetString("magnet-pickup-activate-verb"),
                Act = () => StartSiphon(ent, user),
            });
            return;
        }

        var enabled = !ent.Comp.Enabled;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString(enabled ? "magnet-pickup-enable-verb" : "magnet-pickup-disable-verb"),
            Act = () =>
            {
                ent.Comp.Enabled = enabled;
                ent.Comp.NextScan = _timing.CurTime;
                Dirty(ent);
            },
        });
    }

    private void StartSiphon(Entity<MagnetPickupComponent> ent, EntityUid user)
    {
        if (!ent.Comp.ManualTriggerOnly || !CanSiphon(ent))
            return;

        var args = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(1), new MagnetPickupDoAfterEvent(), ent, target: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
            AttemptFrequency = AttemptFrequency.EveryTick,
        };
        _doAfter.TryStartDoAfter(args);
    }

    private void OnDoAfterAttempt(Entity<MagnetPickupComponent> ent, ref DoAfterAttemptEvent<MagnetPickupDoAfterEvent> args)
    {
        if (!ent.Comp.ManualTriggerOnly || !CanSiphon(ent))
            args.Cancel();
    }

    private void OnDoAfter(Entity<MagnetPickupComponent> ent, ref MagnetPickupDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !ent.Comp.ManualTriggerOnly || !CanSiphon(ent))
            return;

        args.Handled = true;
        Siphon(ent);
    }

    private bool CanSiphon(Entity<MagnetPickupComponent> ent)
    {
        SharedApcPowerReceiverComponent? receiver = null;
        if (ent.Comp.RequirePowered && (!_power.ResolveApc(ent, ref receiver) || !receiver.Powered))
            return false;

        var xform = Transform(ent);
        if (ent.Comp.RequireActiveHand && (!_hands.TryGetActiveItem(xform.ParentUid, out var activeItem) || activeItem != ent.Owner))
            return false;

        if (ent.Comp.SlotFlags != null)
        {
            if (!_inventory.TryGetContainingSlot((ent.Owner, xform, MetaData(ent)), out var slotDef))
                return false;

            if ((slotDef.SlotFlags & ent.Comp.SlotFlags) == 0x0)
                return false;
        }

        return !TryComp<StorageComponent>(ent, out var storage) || _storage.HasSpace((ent.Owner, storage));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<MagnetPickupComponent>();
        var currentTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.ManualTriggerOnly || !comp.Enabled || comp.NextScan > currentTime)
                continue;

            comp.NextScan += ScanDelay;
            Dirty(uid, comp);

            if (CanSiphon((uid, comp)))
                Siphon((uid, comp));
        }
    }

    private void Siphon(Entity<MagnetPickupComponent> ent)
    {
        var parentUid = Transform(ent).ParentUid;
        var playedSound = false;
        _nearby.Clear();
        _nearby.AddRange(_lookup.GetEntitiesInRange(ent, ent.Comp.Range, LookupFlags.Dynamic | LookupFlags.Sundries));
        _nearby.Sort((a, b) => GetNetEntity(a).CompareTo(GetNetEntity(b)));

        foreach (var near in _nearby)
        {
            if (TerminatingOrDeleted(near) || EntityManager.IsQueuedForDeletion(near))
                continue;

            if (!_physicsQuery.TryGetComponent(near, out var physics) || physics.BodyStatus != BodyStatus.OnGround)
                continue;

            if (near == ent.Owner || near == parentUid)
                continue;

            var ev = new MagnetPickupAttemptEvent(near, !playedSound);
            RaiseLocalEvent(ent, ref ev);
            playedSound |= ev.Handled;
        }
    }
}
