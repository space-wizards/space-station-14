using Content.Server.Cuffs;
using Content.Server.Forensics;
using Content.Server.Humanoid;
using Content.Server.Implants.Components;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.Cuffs.Components;
using Content.Shared.Humanoid;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using System.Numerics;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Store.Components;
using Robust.Shared.Collections;
using Robust.Shared.Map.Components;

namespace Content.Server.Implants;

public sealed class SubdermalImplantSystem : SharedSubdermalImplantSystem
{
    [Dependency] private readonly CuffableSystem _cuffable = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly ForensicsSystem _forensicsSystem = default!;
    [Dependency] private readonly PullingSystem _pullingSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private HashSet<Entity<MapGridComponent>> _targetGrids = [];

    public override void Initialize()
    {
        base.Initialize();

        _physicsQuery = GetEntityQuery<PhysicsComponent>();

        SubscribeLocalEvent<SubdermalImplantComponent, UseFreedomImplantEvent>(OnFreedomImplant);
        SubscribeLocalEvent<StoreComponent, ImplantRelayEvent<AfterInteractUsingEvent>>(OnStoreRelay);
        SubscribeLocalEvent<SubdermalImplantComponent, ActivateImplantEvent>(OnActivateImplantEvent);
        SubscribeLocalEvent<SubdermalImplantComponent, UseScramImplantEvent>(OnScramImplant);
        SubscribeLocalEvent<SubdermalImplantComponent, UseDnaScramblerImplantEvent>(OnDnaScramblerImplant);

    }

    private void OnStoreRelay(EntityUid uid, StoreComponent store, ImplantRelayEvent<AfterInteractUsingEvent> implantRelay)
    {
        var args = implantRelay.Event;

        if (args.Handled)
            return;

        // can only insert into yourself to prevent uplink checking with renault
        if (args.Target != args.User)
            return;

        if (!TryComp<CurrencyComponent>(args.Used, out var currency))
            return;

        // same as store code, but message is only shown to yourself
        args.Handled = _store.TryAddCurrency(_store.GetCurrencyValue(args.Used, currency), uid, store);

        if (!args.Handled)
            return;

        var msg = Loc.GetString("store-currency-inserted-implant", ("used", args.Used));
        _popup.PopupEntity(msg, args.User, args.User);
        QueueDel(args.Used);
    }

    private void OnFreedomImplant(EntityUid uid, SubdermalImplantComponent component, UseFreedomImplantEvent args)
    {
        if (!TryComp<CuffableComponent>(component.ImplantedEntity, out var cuffs) || cuffs.Container.ContainedEntities.Count < 1)
            return;

        _cuffable.Uncuff(component.ImplantedEntity.Value, cuffs.LastAddedCuffs, cuffs.LastAddedCuffs);
        args.Handled = true;
    }

    private void OnActivateImplantEvent(EntityUid uid, SubdermalImplantComponent component, ActivateImplantEvent args)
    {
        args.Handled = true;
    }

    private void OnScramImplant(EntityUid uid, SubdermalImplantComponent component, UseScramImplantEvent args)
    {
        if (component.ImplantedEntity is not { } ent)
            return;

        if (!TryComp<ScramImplantComponent>(uid, out var implant))
            return;

        // We need stop the user from being pulled so they don't just get "attached" with whoever is pulling them.
        // This can for example happen when the user is cuffed and being pulled.
        if (TryComp<PullableComponent>(ent, out var pull) && _pullingSystem.IsPulled(ent, pull))
            _pullingSystem.TryStopPull(ent, pull);

        var xform = Transform(ent);
        var targetCoords = SelectRandomTileInRange(xform, implant.TeleportRadius);

        if (targetCoords != null)
        {
            _xform.SetCoordinates(ent, targetCoords.Value);
            _audio.PlayPvs(implant.TeleportSound, ent);
            args.Handled = true;
        }
    }

    private EntityCoordinates? SelectRandomTileInRange(TransformComponent userXform, float radius)
    {
        var userCoords = userXform.Coordinates.ToMap(EntityManager, _xform);
        _targetGrids.Clear();
        _lookupSystem.GetEntitiesInRange(userCoords, radius, _targetGrids);
        Entity<MapGridComponent>? targetGrid = null;

        if (_targetGrids.Count == 0)
            return null;

        // Give preference to the grid the entity is currently on.
        // This does not guarantee that if the probability fails that the owner's grid won't be picked.
        // In reality the probability is higher and depends on the number of grids.
        if (userXform.GridUid != null && TryComp<MapGridComponent>(userXform.GridUid, out var gridComp))
        {
            var userGrid = new Entity<MapGridComponent>(userXform.GridUid.Value, gridComp);
            if (_random.Prob(0.5f))
            {
                _targetGrids.Remove(userGrid);
                targetGrid = userGrid;
            }
        }

        if (targetGrid == null)
            targetGrid = _random.GetRandom().PickAndTake(_targetGrids);

        EntityCoordinates? targetCoords = null;

        do
        {
            var valid = false;

            var range = (float) Math.Sqrt(radius);
            var box = Box2.CenteredAround(userCoords.Position, new Vector2(range, range));
            var tilesInRange = _mapSystem.GetTilesEnumerator(targetGrid.Value.Owner, targetGrid.Value.Comp, box, false);
            var tileList = new ValueList<Vector2i>();

            while (tilesInRange.MoveNext(out var tile))
            {
                tileList.Add(tile.GridIndices);
            }

            while (tileList.Count != 0)
            {
                var tile = tileList.RemoveSwap(_random.Next(tileList.Count));
                valid = true;
                foreach (var entity in _mapSystem.GetAnchoredEntities(targetGrid.Value.Owner, targetGrid.Value.Comp,
                             tile))
                {
                    if (!_physicsQuery.TryGetComponent(entity, out var body))
                        continue;

                    if (body.BodyType != BodyType.Static ||
                        !body.Hard ||
                        (body.CollisionLayer & (int) CollisionGroup.MobMask) == 0)
                        continue;

                    valid = false;
                    break;
                }

                if (valid)
                {
                    targetCoords = new EntityCoordinates(targetGrid.Value.Owner,
                        _mapSystem.TileCenterToVector(targetGrid.Value, tile));
                    break;
                }
            }

            if (valid || _targetGrids.Count == 0) // if we don't do the check here then PickAndTake will blow up on an empty set.
                break;

            targetGrid = _random.GetRandom().PickAndTake(_targetGrids);
        } while (true);

        return targetCoords;
    }

    private void OnDnaScramblerImplant(EntityUid uid, SubdermalImplantComponent component, UseDnaScramblerImplantEvent args)
    {
        if (component.ImplantedEntity is not { } ent)
            return;

        if (TryComp<HumanoidAppearanceComponent>(ent, out var humanoid))
        {
            var newProfile = HumanoidCharacterProfile.RandomWithSpecies(humanoid.Species);
            _humanoidAppearance.LoadProfile(ent, newProfile, humanoid);
            _metaData.SetEntityName(ent, newProfile.Name);
            if (TryComp<DnaComponent>(ent, out var dna))
            {
                dna.DNA = _forensicsSystem.GenerateDNA();
            }
            if (TryComp<FingerprintComponent>(ent, out var fingerprint))
            {
                fingerprint.Fingerprint = _forensicsSystem.GenerateFingerprint();
            }
            _popup.PopupEntity(Loc.GetString("scramble-implant-activated-popup"), ent, ent);
        }

        args.Handled = true;
        QueueDel(uid);
    }
}
