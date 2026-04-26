using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration.Logs;
using Content.Shared.Construction.Components;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Materials;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using JetBrains.Annotations;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Construction;

public abstract class SharedFlatpackSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] private readonly AnchorableSystem _anchorable = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] protected readonly MachinePartSystem MachinePart = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly SharedMaterialStorageSystem MaterialStorage = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<FlatpackComponent, InteractUsingEvent>(OnFlatpackInteractUsing);
        SubscribeLocalEvent<FlatpackComponent, ActivateInWorldEvent>(OnFlatpackActivateInWorld);
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
        if (args.Handled)
            return;

        args.Handled = Unpack(ent, args.User, args.Used, out _);
    }

    private void OnFlatpackActivateInWorld(Entity<FlatpackComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = Unpack(ent, args.User, used: null, out _);
    }

    private void OnFlatpackExamined(Entity<FlatpackComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (ent.Comp.QualityNeeded is { } qualityNeeded)
        {
            if (PrototypeManager.Resolve(qualityNeeded, out var quality))
            {
                args.PushMarkup(Loc.GetString("flatpack-examine", ("qualityNeeded", Loc.GetString(quality.Name))));
            }
        }
        else
        {
            args.PushMarkup(Loc.GetString("flatpack-examine", ("qualityNeeded", "NO-TOOL")));
        }
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
    /// Attempts to unpack <paramref name="flatpack"/> at its current location.
    /// </summary>
    /// <param name="flatpack">The flatpack to unpack</param>
    /// <param name="user">The entity which is unpacking the flatpack; used for logging and player interaction feedback</param>
    /// <param name="used">The entity being used to unpack, usually a tool</param>
    /// <param name="unpacked">The entity which is created by a successful unpacking. May be client-side-predicted</param>
    /// <returns>
    /// Whether or not interaction with the flatpack occurred. Note that a true return <b>does not</b> imply
    /// <paramref name="unpacked"/> is not null. In the case that the correct tool is used to on the flatpack but there
    /// is not enough space to unpack, this will be true. This is used to enable event-handlers to correctly set
    /// <see cref="HandledEntityEventArgs.Handled"/>.
    /// </returns>
    [PublicAPI]
    public bool Unpack(
        Entity<FlatpackComponent> flatpack,
        EntityUid user,
        Entity<ToolComponent?>? used,
        out EntityUid? unpacked
    )
    {
        unpacked = null;

        if (flatpack.Comp.QualityNeeded is { } qualityNeeded)
        {
            if (used is not {} u || !_tool.HasQuality(u, qualityNeeded, u.Comp))
            {
                return false;
            }
        }
        else if (used != null)
        {
            return false;
        }

        if (_container.IsEntityInContainer(flatpack))
            return false;

        var xform = Transform(flatpack);

        if (xform.GridUid is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return false;

        if (flatpack.Comp.Entity == null)
        {
            Log.Error($"No entity prototype present for flatpack {ToPrettyString(flatpack)}.");
            PredictedQueueDel(flatpack);
            return true;
        }

        if (!PrototypeManager.Resolve(flatpack.Comp.Entity, out var proto) ||
            !proto.TryGetComponent<FixturesComponent>(out var fixture, EntityManager.ComponentFactory))
        {
            return true;
        }

        var (layer, mask) = SharedPhysicsSystem.GetHardCollision(fixture);
        var buildPos = _map.TileIndicesFor(grid, gridComp, xform.Coordinates);

        if (!_anchorable.TileFree((grid, gridComp), buildPos, layer, mask))
        {
            _popup.PopupPredicted(Loc.GetString("flatpack-unpack-no-room"), flatpack, user);
            return true;
        }

        unpacked = PredictedSpawnAtPosition(flatpack.Comp.Entity, _map.GridTileToLocal(grid, gridComp, buildPos));
        _adminLogger.Add(LogType.Construction,
            LogImpact.Low,
            $"{ToPrettyString(user):player} unpacked {ToPrettyString(unpacked):entity} at {xform.Coordinates} from {ToPrettyString(flatpack):entity}");
        PredictedQueueDel(flatpack);

        _audio.PlayPredicted(flatpack.Comp.UnpackSound, used?.Owner ?? flatpack, user);
        return true;
    }
}
