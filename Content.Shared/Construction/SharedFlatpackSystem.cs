using System.Numerics;
using Content.Shared.Construction.Components;
using Content.Shared.Administration.Logs;
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
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Construction;

public abstract class SharedFlatpackSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly INetManager _net = default!;
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

        SubscribeLocalEvent<FlatpackCreatorComponent, ContainerIsRemovingAttemptEvent>(OnCreatorRemovingAttempt);
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

        var intersecting = _entityLookup.GetEntitiesIntersecting(coords, LookupFlags.Dynamic | LookupFlags.Static);

        // todo make this logic smarter.
        // This should eventually allow for shit like building microwaves on tables and such.
        foreach (var intersect in intersecting)
        {
            if (!TryComp<PhysicsComponent>(intersect, out var intersectBody))
                continue;

            if (!intersectBody.Hard || !intersectBody.CanCollide)
                continue;

            // this popup is on the server because the mispredicts on the intersection is crazy
            if (_net.IsServer)
                _popup.PopupEntity(Loc.GetString("flatpack-unpack-no-room"), uid, args.User);
            return;
        }

        if (_net.IsServer)
        {
            var spawn = Spawn(comp.Entity, _map.GridTileToLocal(grid, gridComp, buildPos));
            _adminLogger.Add(LogType.Construction, LogImpact.Low,
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

    private void OnCreatorRemovingAttempt(Entity<FlatpackCreatorComponent> ent, ref ContainerIsRemovingAttemptEvent args)
    {
        if (args.Container.ID == ent.Comp.SlotId && ent.Comp.Packing)
            args.Cancel();
    }

    public void SetupFlatpack(Entity<FlatpackComponent?> ent, EntityUid? board)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        EntProtoId machinePrototypeId;
        string? entityPrototype;
        if (TryComp<MachineBoardComponent>(board, out var machineBoard) && machineBoard.Prototype is not null)
            machinePrototypeId = machineBoard.Prototype;
        else if (TryComp<ComputerBoardComponent>(board, out var computerBoard) && computerBoard.Prototype is not null)
            machinePrototypeId = computerBoard.Prototype;

        var comp = ent.Comp!;
        var machinePrototype = PrototypeManager.Index(machinePrototypeId);

        var meta = MetaData(ent);
        _metaData.SetEntityName(ent, Loc.GetString("flatpack-entity-name", ("name", machinePrototype.Name)), meta);
        _metaData.SetEntityDescription(ent, Loc.GetString("flatpack-entity-description", ("name", machinePrototype.Name)), meta);

        comp.Entity = machinePrototypeId;
        Dirty(ent, comp);

        if (board is null)
            return;

        Appearance.SetData(ent, FlatpackVisuals.Machine, MetaData(board.Value).EntityPrototype?.ID ?? string.Empty);
    }

    public Dictionary<string, int> GetFlatpackCreationCost(Entity<FlatpackCreatorComponent> entity, Entity<MachineBoardComponent>? machineBoard = null)
    {
        Dictionary<string, int> cost = new();
        Dictionary<ProtoId<MaterialPrototype>, int> baseCost;
        if (machineBoard is not null)
        {
            cost = MachinePart.GetMachineBoardMaterialCost(machineBoard.Value, -1);
            baseCost = entity.Comp.BaseMachineCost;
        }
        else
            baseCost = entity.Comp.BaseComputerCost;

        foreach (var (mat, amount) in baseCost)
        {
            cost.TryAdd(mat, 0);
            cost[mat] -= amount;
        }

        return cost;
    }
}
