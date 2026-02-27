using System.Diagnostics.CodeAnalysis;
using Content.Shared.Construction.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Materials;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;

namespace Content.Shared.Construction;

public abstract class SharedFlatpackSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] protected readonly MachinePartSystem MachinePart = default!;
    [Dependency] protected readonly SharedMaterialStorageSystem MaterialStorage = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<FlatpackComponent, InteractUsingEvent>(OnFlatpackInteractUsing);
        SubscribeLocalEvent<FlatpackComponent, ExaminedEvent>(OnFlatpackExamined);

        SubscribeLocalEvent<FlatpackCreatorComponent, ItemSlotInsertAttemptEvent>(OnInsertAttempt);
    }

    private void OnInsertAttempt(Entity<FlatpackCreatorComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Slot.ID != ent.Comp.SlotId || args.Cancelled)
            return;

        if (HasComp<MachineBoardComponent>(args.Item))
            return;

        if (TryComp<ComputerBoardComponent>(args.Item, out var computer) && computer.Prototype != null)
            return;

        args.Cancelled = true;
    }

    private void OnFlatpackInteractUsing(Entity<FlatpackComponent> ent, ref InteractUsingEvent args)
    {
        var (uid, comp) = ent;
        if (!_tool.HasQuality(args.Used, comp.QualityNeeded) || _container.IsEntityInContainer(ent))
            return;

        var xform = Transform(ent);

        if (xform.GridUid is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        args.Handled = true;

        if (comp.Entity == null)
        {
            Log.Error($"No entity prototype present for flatpack {ToPrettyString(ent)}.");

            if (_net.IsServer)
                QueueDel(ent);
            return;
        }

        var buildPos = _map.TileIndicesFor(grid, gridComp, xform.Coordinates);
        var coords = _map.ToCenterCoordinates(grid, buildPos);

        if (!HasRoomToConstruct(comp.Entity.Value, coords))
        {
            // this popup is on the server because the predicts on the intersection is crazy
            if (_net.IsServer)
                _popup.PopupEntity(Loc.GetString("flatpack-unpack-no-room"), uid, args.User);
            return;
        }

        if (_net.IsServer)
        {
            var spawn = Spawn(comp.Entity, _map.GridTileToLocal(grid, gridComp, buildPos));
            _adminLogger.Add(LogType.Construction,
                LogImpact.Low,
                $"{ToPrettyString(args.User):player} unpacked {ToPrettyString(spawn):entity} at {xform.Coordinates} from {ToPrettyString(uid):entity}");
            QueueDel(uid);
        }

        _audio.PlayPredicted(comp.UnpackSound, args.Used, args.User);
    }

    private void OnFlatpackExamined(Entity<FlatpackComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;
        args.PushMarkup(Loc.GetString("flatpack-examine"));
    }

    protected void SetupFlatpack(Entity<FlatpackComponent?> ent, EntProtoId proto, EntityUid board)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Entity = proto;
        var machinePrototype = PrototypeManager.Index<EntityPrototype>(proto);

        var meta = MetaData(ent);
        _metaData.SetEntityName(ent, Loc.GetString("flatpack-entity-name", ("name", machinePrototype.Name)), meta);
        _metaData.SetEntityDescription(ent, Loc.GetString("flatpack-entity-description", ("name", machinePrototype.Name)), meta);

        Dirty(ent, meta);
        Appearance.SetData(ent, FlatpackVisuals.Machine, MetaData(board).EntityPrototype?.ID ?? string.Empty);
    }

    /// <summary>
    /// Returns the prototype from a board that the flatpacker will create.
    /// </summary>
    public bool TryGetFlatpackResultPrototype(EntityUid board, [NotNullWhen(true)] out EntProtoId? prototype)
    {
        prototype = null;

        if (TryComp<MachineBoardComponent>(board, out var machine))
            prototype = machine.Prototype;
        else if (TryComp<ComputerBoardComponent>(board, out var computer))
            prototype = computer.Prototype;
        return prototype is not null;
    }

    /// <summary>
    /// Tries to get the cost to produce an item, fails if unable to produce it.
    /// </summary>
    /// <param name="entity">The flatpacking machine</param>
    /// <param name="machineBoard">The machine board to pack. If null, this implies we are packing a computer board</param>
    /// <param name="cost">Cost to produce</param>
    public bool TryGetFlatpackCreationCost(Entity<FlatpackCreatorComponent> entity, EntityUid machineBoard, out Dictionary<string, int> cost)
    {
        cost = new();
        Dictionary<ProtoId<MaterialPrototype>, int> baseCost;
        if (TryComp<MachineBoardComponent>(machineBoard, out var machineBoardComp))
        {
            if (!MachinePart.TryGetMachineBoardMaterialCost((machineBoard, machineBoardComp), out cost, -1))
                return false;
            baseCost = entity.Comp.BaseMachineCost;
        }
        else
            baseCost = entity.Comp.BaseComputerCost;

        foreach (var (mat, amount) in baseCost)
        {
            cost.TryAdd(mat, 0);
            cost[mat] -= amount;
        }

        return true;
    }

    /// <summary>
    /// Checks if there is room to construct a prototype at specified coordinates,
    /// checking collison layers.
    /// </summary>
    /// <param name="protoId">The prototype being constructed</param>
    /// <param name="coords">Location that protoId is being constructed</param>
    private bool HasRoomToConstruct(EntProtoId protoId, EntityCoordinates coords)
    {
        var entWithPhysics = new EntProtoId<FixturesComponent>(protoId);
        if (!entWithPhysics.TryGet(out var flatpackFixtures, PrototypeManager, _componentFactory))
        {
            // The entity we're constructing has no fixtures, so can't overlap anything
            return true;
        }

        // Sum up the collision mask of all the fixtures of the entity we're trying to create
        int collisionMask = 0;
        foreach (var fixture in flatpackFixtures.Fixtures.Values)
        {
            if (fixture.Hard)
            {
                collisionMask |= fixture.CollisionMask;
            }
        }

        // Find all the overlaps at the coordinates and see if they have a collision
        // layer which collides with any of the flatpacked entity's collision mask
        // Note, we're not doing a proper narrowphase test against every fixture pair
        // here, just checking for any fixture at the specified coordinates.
        var overlaps = _entityLookup.GetEntitiesIntersecting(coords, LookupFlags.Static | LookupFlags.Dynamic);
        foreach (var overlap in overlaps)
        {
            if (!TryComp<FixturesComponent>(overlap, out var overlapFixtures))
            {
                // This overlap has no fixtures. Can't collide.
                continue;
            }

            foreach (var fixture in overlapFixtures.Fixtures.Values)
            {
                if (fixture.Hard && (fixture.CollisionLayer & collisionMask) != 0)
                {
                    // Found an overlap which collides with at least one of the
                    // flatpacked entity's fixtures.
                    return false;
                }
            }
        }

        return true;
    }
}
