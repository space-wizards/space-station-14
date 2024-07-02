using Content.Server.Storage.Components;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Storage.EntitySystems;

/// <summary>
/// <see cref="MagnetPickupComponent"/>
/// </summary>
public sealed class MagnetPickupSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;


    private static readonly TimeSpan ScanDelay = TimeSpan.FromSeconds(1);

    private EntityQuery<PhysicsComponent> _physicsQuery;

    public override void Initialize()
    {
        base.Initialize();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        SubscribeLocalEvent<MagnetPickupComponent, MapInitEvent>(OnMagnetMapInit);
        SubscribeLocalEvent<MagnetPickupComponent, GetVerbsEvent<AlternativeVerb>>(AddToggleMagnetVerb);
        SubscribeLocalEvent<MagnetPickupComponent, ExaminedEvent>(OnExamine);
    }

    private void OnMagnetMapInit(EntityUid uid, MagnetPickupComponent component, MapInitEvent args)
    {
        component.NextScan = _timing.CurTime;
    }

    private void AddToggleMagnetVerb(EntityUid uid, MagnetPickupComponent component, GetVerbsEvent<AlternativeVerb> args)
    {

        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () =>
                    {
                        var magnetStateText = ToggleMagnet(uid, component) ? Loc.GetString("comp-magnet-pickup-state-on") : Loc.GetString("comp-magnet-pickup-state-off");
                        var popupText = Loc.GetString("comp-magnet-pickup-toggle-popup", ("state", magnetStateText));
                        _popup.PopupClient(popupText, uid, args.User);
                    },
            Text = Loc.GetString("comp-magnet-pickup-toggle-verb")
        });
    }
    private bool ToggleMagnet(EntityUid uid, MagnetPickupComponent component)
    {
        component.Enabled = !component.Enabled;
        Dirty(uid, component);
        return component.Enabled;
    }
    private void OnExamine(EntityUid uid, MagnetPickupComponent component, ExaminedEvent args)
    {
        var magnetStateText = component.Enabled ? Loc.GetString("comp-magnet-pickup-state-on-colored") : Loc.GetString("comp-magnet-pickup-state-off-colored");
        var examineText = Loc.GetString("comp-magnet-pickup-examine", ("state", magnetStateText));
        args.PushMarkup(examineText);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<MagnetPickupComponent, StorageComponent, TransformComponent, MetaDataComponent>();
        var currentTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var comp, out var storage, out var xform, out var meta))
        {
            if (!comp.Enabled)
                continue;

            if (comp.NextScan > currentTime)
                continue;

            comp.NextScan += ScanDelay;

            // "slotFlags: NONE" in yaml can be used to bypass inventory slot checking
            if (comp.SlotFlags != SlotFlags.NONE)
            {
                if (!_inventory.TryGetContainingSlot((uid, xform, meta), out var slotDef))
                    continue;

                if ((slotDef.SlotFlags & comp.SlotFlags) == 0x0)
                    continue;
            }


            // No space
            if (!_storage.HasSpace((uid, storage)))
                continue;

            var parentUid = xform.ParentUid;
            var playedSound = false;
            var finalCoords = xform.Coordinates;
            var moverCoords = _transform.GetMoverCoordinates(uid, xform);

            foreach (var near in _lookup.GetEntitiesInRange(uid, comp.Range, LookupFlags.Dynamic | LookupFlags.Sundries))
            {
                if (_whitelistSystem.IsWhitelistFail(storage.Whitelist, near))
                    continue;

                if (!_physicsQuery.TryGetComponent(near, out var physics) || physics.BodyStatus != BodyStatus.OnGround)
                    continue;

                if (near == parentUid)
                    continue;

                // TODO: Probably move this to storage somewhere when it gets cleaned up
                // TODO: This sucks but you need to fix a lot of stuff to make it better
                // the problem is that stack pickups delete the original entity, which is fine, but due to
                // game state handling we can't show a lerp animation for it.
                var nearXform = Transform(near);
                var nearMap = _transform.GetMapCoordinates(near, xform: nearXform);
                var nearCoords = EntityCoordinates.FromMap(moverCoords.EntityId, nearMap, _transform, EntityManager);

                if (!_storage.Insert(uid, near, out var stacked, storageComp: storage, playSound: !playedSound))
                    continue;

                // Play pickup animation for either the stack entity or the original entity.
                if (stacked != null)
                    _storage.PlayPickupAnimation(stacked.Value, nearCoords, finalCoords, nearXform.LocalRotation);
                else
                    _storage.PlayPickupAnimation(near, nearCoords, finalCoords, nearXform.LocalRotation);

                playedSound = true;
            }
        }
    }
}
