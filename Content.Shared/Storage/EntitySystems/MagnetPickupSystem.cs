using Content.Shared.Hands.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Interaction;
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
    [Dependency] private SharedInteractionSystem _interaction = default!;

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
    }

    private void OnMagnetMapInit(EntityUid uid, MagnetPickupComponent component, MapInitEvent args)
    {
        component.NextScan = _timing.CurTime;
    }

    private void OnExamined(Entity<MagnetPickupComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(ent.Comp.Enabled ? "magnet-pickup-examine-on" : "magnet-pickup-examine-off"));
    }

    private void OnGetVerbs(Entity<MagnetPickupComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<MagnetPickupComponent, TransformComponent, MetaDataComponent>();
        var currentTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var comp, out var xform, out var meta))
        {
            if (!comp.Enabled || comp.NextScan > currentTime)
                continue;

            comp.NextScan += ScanDelay;
            Dirty(uid, comp);

            var parentUid = xform.ParentUid;

            if (comp.RequireAnchored && !xform.Anchored)
                continue;

            if (comp.RequireActiveHand && (!_hands.TryGetActiveItem(parentUid, out var activeItem) || activeItem != uid))
                continue;

            if (comp.SlotFlags != null)
            {
                if (!_inventory.TryGetContainingSlot((uid, xform, meta), out var slotDef))
                    continue;

                if ((slotDef.SlotFlags & comp.SlotFlags) == 0x0)
                    continue;
            }

            // No space
            TryComp<StorageComponent>(uid, out var storage);
            if (storage != null && !_storage.HasSpace((uid, storage)))
                continue;

            var playedSound = false;
            _nearby.Clear();
            _nearby.AddRange(_lookup.GetEntitiesInRange(uid, comp.Range, LookupFlags.Dynamic | LookupFlags.Sundries));
            _nearby.Sort((a, b) => GetNetEntity(a).CompareTo(GetNetEntity(b)));

            foreach (var near in _nearby)
            {
                if (TerminatingOrDeleted(near) || EntityManager.IsQueuedForDeletion(near))
                    continue;

                if (!_physicsQuery.TryGetComponent(near, out var physics) || physics.BodyStatus != BodyStatus.OnGround)
                    continue;

                if (near == uid || near == parentUid)
                    continue;


                var ev = new MagnetPickupAttemptEvent(near, !playedSound);
                RaiseLocalEvent(uid, ref ev);
                playedSound |= ev.Handled;
            }
        }
    }
}
