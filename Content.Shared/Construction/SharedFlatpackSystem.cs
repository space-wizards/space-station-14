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
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
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
    [Dependency] private readonly EntityQuery<MapGridComponent> _mapGridQuery = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<FlatpackComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
        SubscribeLocalEvent<FlatpackComponent, GetVerbsEvent<ActivationVerb>>(OnGetActivationVerbs);
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

    private void OnGetInteractionVerbs(Entity<FlatpackComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        // Interaction is only allowed for tool-required unpacks.
        if (ent.Comp.QualityNeeded is not { } qualityNeeded ||
            !args.CanAccess || !args.CanInteract || !args.CanComplexInteract ||
            !PrototypeManager.Resolve(qualityNeeded, out var quality))
            return;

        var user = args.User;
        var disabled = args.Using is not { } used || !_tool.HasQuality(used, qualityNeeded);
        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("flatpack-unpack-verb-text"),
            Icon = quality.Icon,
            Act = () => Unpack(ent, user),
            Disabled = disabled,
            Message = disabled
                ? (string?)Loc.GetString(
                    "flatpack-unpack-verb-need-tool-message",
                    ("toolNeeded", Loc.GetString(quality.ToolName))
                )
                : null,
        });
    }

    private void OnGetActivationVerbs(Entity<FlatpackComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        // Activation is only allowed for tool-less unpacks.
        if (ent.Comp.QualityNeeded != null ||
            !args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        var user = args.User;
        args.Verbs.Add(new ActivationVerb
        {
            Text = Loc.GetString("flatpack-unpack-verb-text"),
            Act = () => Unpack(ent, user),
        });
    }

    private void OnFlatpackInteractUsing(Entity<FlatpackComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled ||
            ent.Comp.QualityNeeded is not { } qualityNeeded ||
            !_tool.HasQuality(args.Used, qualityNeeded))
            return;

        Unpack(ent, args.User);
        args.Handled = true;
    }

    private void OnFlatpackActivateInWorld(Entity<FlatpackComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex || ent.Comp.QualityNeeded != null)
            return;

        Unpack(ent, args.User);
        args.Handled = true;
    }

    private void OnFlatpackExamined(Entity<FlatpackComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (ent.Comp.QualityNeeded is { } qualityNeeded &&
            PrototypeManager.Resolve(qualityNeeded, out var quality))
        {
            args.PushMarkup(Loc.GetString("flatpack-examine", ("toolNeeded", Loc.GetString(quality.ToolName))));
        }
        else
        {
            args.PushMarkup(Loc.GetString("flatpack-examine-no-tool-needed"));
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
        _metaData.SetEntityDescription(
            ent,
            Loc.GetString("flatpack-entity-description", ("name", machinePrototype.Name)),
            meta
        );

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

    /// <summary>Attempts to unpack <paramref name="flatpack"/> at its current location.</summary>
    /// <param name="flatpack">The flatpack to unpack</param>
    /// <param name="user">The entity which is unpacking the flatpack; used for logging and player interaction feedback</param>
    /// <param name="playAudio">If true, will play <see cref="FlatpackComponent.UnpackSound"/> on successful unpacking</param>
    /// <returns>
    /// The entity which is created by a successful unpacking. May be client-side-predicted. Returns null in the case
    /// that unpacking failed.
    /// </returns>
    [PublicAPI]
    public EntityUid? Unpack(
        Entity<FlatpackComponent> flatpack,
        EntityUid? user,
        bool playAudio = true
    )
    {
        if (_container.IsEntityInContainer(flatpack))
        {
            return null;
        }

        var xform = Transform(flatpack);
        if (xform.GridUid is not { } grid || !_mapGridQuery.TryComp(grid, out var gridComp))
        {
            return null;
        }

        if (!PrototypeManager.Resolve(flatpack.Comp.Entity, out var proto) ||
            !proto.TryGetComponent<FixturesComponent>(out var fixture, EntityManager.ComponentFactory))
        {
            return null;
        }

        var (layer, mask) = SharedPhysicsSystem.GetHardCollision(fixture);
        var buildPos = _map.TileIndicesFor(grid, gridComp, xform.Coordinates);

        if (!_anchorable.TileFree((grid, gridComp), buildPos, layer, mask))
        {
            _popup.PopupPredicted(Loc.GetString("flatpack-unpack-no-room"), flatpack, user);
            return null;
        }

        var coords = _map.GridTileToLocal(grid, gridComp, buildPos);
        var spawned = Unpack(flatpack, coords, user, playAudio);

        return spawned;
    }

    /// <summary>
    /// Spawns the <paramref name="flatpack"/>'s <see cref="FlatpackComponent.Entity"/> at <paramref name="coords"/>,
    /// deletes the flatpack, and plays unpacking audio for the <paramref name="user"/>.
    /// </summary>
    private EntityUid Unpack(
        Entity<FlatpackComponent> flatpack,
        EntityCoordinates coords,
        EntityUid? user,
        bool playAudio = true
    )
    {
        var spawned = PredictedSpawnAttachedTo(flatpack.Comp.Entity, coords);
        if (user != null)
        {
            _adminLogger.Add(
                LogType.Construction,
                LogImpact.Low,
                $"{user} unpacked {spawned} at {coords} from {flatpack}"
            );
        }

        PredictedQueueDel(flatpack);

        if (playAudio)
        {
            _audio.PlayPredicted(flatpack.Comp.UnpackSound, spawned, user);
        }

        return spawned;
    }
}
