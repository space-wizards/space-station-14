using System.Linq;
using System.Numerics;
using Content.Server.Actions;
using Content.Server.Backmen.Blob.Components;
using Content.Server.Backmen.GameTicking.Rules.Components;
using Content.Server.Chat.Managers;
using Content.Server.Hands.Systems;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Backmen.Blob;
using Content.Shared.Backmen.Blob.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Hands.Components;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.CPUJob.JobQueues.Queues;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Backmen.Blob;

public sealed class BlobObserverSystem : SharedBlobObserverSystem
{
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly BlobCoreSystem _blobCoreSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly RoleSystem _roleSystem = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly ISharedPlayerManager _actorSystem = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriberSystem = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly HandsSystem _hands = default!;

    private EntityQuery<BlobTileComponent> _tileQuery;

    private const double MoverJobTime = 0.005;
    private readonly JobQueue _moveJobQueue = new(MoverJobTime);

    [Dependency] private ILogManager _logMan = default!;
    private ISawmill _logger = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobCoreComponent, CreateBlobObserverEvent>(OnCreateBlobObserver);

        SubscribeLocalEvent<BlobObserverComponent, PlayerAttachedEvent>(OnPlayerAttached, before: new []{ typeof(ActionsSystem) });
        SubscribeLocalEvent<BlobObserverComponent, PlayerDetachedEvent>(OnPlayerDetached, before: new []{ typeof(ActionsSystem) });

        SubscribeLocalEvent<BlobCoreComponent, BlobCreateFactoryActionEvent>(OnCreateFactory);
        SubscribeLocalEvent<BlobCoreComponent, BlobCreateResourceActionEvent>(OnCreateResource);
        SubscribeLocalEvent<BlobCoreComponent, BlobCreateNodeActionEvent>(OnCreateNode);
        SubscribeLocalEvent<BlobCoreComponent, BlobCreateBlobbernautActionEvent>(OnCreateBlobbernaut);
        SubscribeLocalEvent<BlobCoreComponent, BlobToCoreActionEvent>(OnBlobToCore);
        SubscribeLocalEvent<BlobCoreComponent, BlobToNodeActionEvent>(OnBlobToNode);
        SubscribeLocalEvent<BlobCoreComponent, BlobSwapChemActionEvent>(OnBlobSwapChem);
        SubscribeLocalEvent<BlobCoreComponent, BlobSwapCoreActionEvent>(OnSwapCore);
        SubscribeLocalEvent<BlobCoreComponent, BlobSplitCoreActionEvent>(OnSplitCore);

        SubscribeLocalEvent<BlobObserverComponent, MoveEvent>(OnMoveEvent);
        SubscribeLocalEvent<BlobObserverComponent, BlobChemSwapPrototypeSelectedMessage>(OnChemSelected);

        SubscribeLocalEvent<BlobObserverComponent, ComponentStartup>(OnStartup);


        _logger = _logMan.GetSawmill("blob.core");
        _tileQuery = GetEntityQuery<BlobTileComponent>();
    }

    [ValidatePrototypeId<EntityPrototype>]
    private const string MobObserverBlobController = "MobObserverBlobController";
    private void OnStartup(Entity<BlobObserverComponent> ent, ref ComponentStartup args)
    {
        _hands.AddHand(ent,"BlobHand",HandLocation.Middle);

        ent.Comp.VirtualItem = Spawn(MobObserverBlobController, Transform(ent).Coordinates);
        var comp = EnsureComp<BlobObserverControllerComponent>(ent.Comp.VirtualItem);
        comp.Blob = ent;
        Dirty(ent);

        if (!_hands.TryPickup(ent, ent.Comp.VirtualItem, "BlobHand", false, false, false))
        {
            QueueDel(ent);
            return;
        }
    }

    private void SendBlobBriefing(EntityUid mind)
    {
        if (_mindSystem.TryGetSession(mind, out var session))
        {
            _chatManager.DispatchServerMessage(session, Loc.GetString("blob-role-greeting"));
        }
    }

    [ValidatePrototypeId<AlertPrototype>]
    private const string BlobHealth = "BlobHealth";

    private void OnCreateBlobObserver(EntityUid blobCoreUid, BlobCoreComponent core, CreateBlobObserverEvent args)
    {
        var observer = Spawn(core.ObserverBlobPrototype, Transform(blobCoreUid).Coordinates);

        core.Observer = observer;

        if (!TryComp<BlobObserverComponent>(observer, out var blobObserverComponent))
        {
            args.Cancel();
            return;
        }

        blobObserverComponent.Core = blobCoreUid;
        Dirty(observer,blobObserverComponent);


        var isNewMind = false;
        if (!_mindSystem.TryGetMind(blobCoreUid, out var mindId, out var mind))
        {
            if (
                !_playerManager.TryGetSessionById(args.UserId, out var playerSession) ||
                playerSession.AttachedEntity == null ||
                !_mindSystem.TryGetMind(playerSession.AttachedEntity.Value, out mindId, out mind))
            {
                mindId = _mindSystem.CreateMind(args.UserId, "Blob Player");
                mind = Comp<MindComponent>(mindId);
                isNewMind = true;
            }
        }

        if (!isNewMind)
        {
            var name = mind.Session?.Name ?? "???";
            _mindSystem.WipeMind(mindId, mind);
            mindId = _mindSystem.CreateMind(args.UserId, $"Blob Player ({name})");
            mind = Comp<MindComponent>(mindId);
            isNewMind = true;
        }

        _roleSystem.MindAddRole(mindId, core.MindRoleBlobPrototypeId.Id);
        SendBlobBriefing(mindId);

        _alerts.ShowAlert(observer, BlobHealth, (short) Math.Clamp(Math.Round(core.CoreBlobTotalHealth.Float() / 10f), 0, 20));

        var blobRule = EntityQuery<BlobRuleComponent>().FirstOrDefault();
        blobRule?.Blobs.Add((mindId,mind));

        _mindSystem.TryAddObjective(mindId, mind, "BlobCaptureObjective");

        _mindSystem.TransferTo(mindId, observer, true, mind: mind);
        if (_actorSystem.TryGetSessionById(args.UserId, out var session))
        {
            _actorSystem.SetAttachedEntity(session, observer, true);
        }

        UpdateUi(observer, core);
    }

    private void UpdateActions(ICommonSession playerSession, EntityUid uid, BlobObserverComponent? component = null)
    {
        if (!Resolve(uid, ref component))
        {
            return;
        }

        if (component.Core == null || TerminatingOrDeleted(component.Core.Value) || !TryComp<BlobCoreComponent>(component.Core.Value, out var coreComponent))
        {
            _logger.Error("Не возможно найти ядро для обсервера!");
            return;
        }

        _action.GrantActions(uid, new []
        {
            coreComponent.ActionSwapBlobChem!.Value,
            coreComponent.ActionTeleportBlobToCore!.Value,
            coreComponent.ActionTeleportBlobToNode!.Value,
            coreComponent.ActionCreateBlobFactory!.Value,
            coreComponent.ActionCreateBlobResource!.Value,
            coreComponent.ActionCreateBlobNode!.Value,
            coreComponent.ActionCreateBlobbernaut!.Value,
            coreComponent.ActionSplitBlobCore!.Value,
            coreComponent.ActionSwapBlobCore!.Value
        }, component.Core.Value);

        _viewSubscriberSystem.AddViewSubscriber(component.Core.Value, playerSession); // GrantActions require keep in pvs
    }

    private void OnPlayerAttached(EntityUid uid, BlobObserverComponent component, PlayerAttachedEvent args)
    {
        UpdateActions(args.Player, uid, component);
    }
    private void OnPlayerDetached(EntityUid uid, BlobObserverComponent component, PlayerDetachedEvent args)
    {
        if (component.Core.HasValue && !TerminatingOrDeleted(component.Core.Value))
        {
            _viewSubscriberSystem.RemoveViewSubscriber(component.Core.Value, args.Player);
        }
    }

    private void OnBlobSwapChem(EntityUid uid, BlobCoreComponent blobCoreComponent,
        BlobSwapChemActionEvent args)
    {
        if (!TryComp<BlobObserverComponent>(args.Performer, out var observerComponent))
            return;

        TryOpenUi(args.Performer, args.Performer, observerComponent);
        args.Handled = true;
    }

    private void OnChemSelected(EntityUid uid, BlobObserverComponent component, BlobChemSwapPrototypeSelectedMessage args)
    {
        if (component.Core == null || !TryComp<BlobCoreComponent>(component.Core.Value, out var blobCoreComponent))
            return;

        if (component.SelectedChemId == args.SelectedId)
            return;

        if (!_blobCoreSystem.TryUseAbility(uid, component.Core.Value, blobCoreComponent,
                blobCoreComponent.SwapChemCost))
            return;

        ChangeChem(uid, args.SelectedId, component);
    }

    private void ChangeChem(EntityUid uid, BlobChemType newChem, BlobObserverComponent component)
    {
        if (component.Core == null || !TryComp<BlobCoreComponent>(component.Core.Value, out var blobCoreComponent))
            return;
        component.SelectedChemId = newChem;
        _blobCoreSystem.ChangeChem(component.Core.Value, newChem, blobCoreComponent);

        _popup.PopupEntity(Loc.GetString("blob-spent-resource", ("point", blobCoreComponent.SwapChemCost)),
            uid,
            uid,
            PopupType.LargeCaution);

        UpdateUi(uid, blobCoreComponent);
    }

    private void TryOpenUi(EntityUid uid, EntityUid user, BlobObserverComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!TryComp(user, out ActorComponent? actor))
            return;

        _uiSystem.TryToggleUi(uid, BlobChemSwapUiKey.Key, actor.PlayerSession);
    }

    public void UpdateUi(EntityUid uid, BlobCoreComponent blobCoreComponent)
    {
        if (!TryComp<BlobObserverComponent>(uid, out var observerComponent))
        {
            return;
        }
        var state = new BlobChemSwapBoundUserInterfaceState(blobCoreComponent.ChemСolors, observerComponent.SelectedChemId);

        _uiSystem.SetUiState(uid, BlobChemSwapUiKey.Key, state);
    }

    // TODO: This is very bad, but it is clearly better than invisible walls, let someone do better.
    private void OnMoveEvent(EntityUid uid, BlobObserverComponent observerComponent, ref MoveEvent args)
    {
        if (observerComponent.IsProcessingMoveEvent)
            return;

        observerComponent.IsProcessingMoveEvent = true;

        var job = new BlobObserverMover(EntityManager, _blocker, _transform,this, MoverJobTime)
        {
            Observer = (uid,observerComponent),
            NewPosition = args.NewPosition
        };

        _moveJobQueue.EnqueueJob(job);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _moveJobQueue.Process();
    }

    public (EntityUid? nearestEntityUid, float nearestDistance) CalculateNearestBlobTileDistance(EntityCoordinates position)
    {
        var nearestDistance = float.MaxValue;
        EntityUid? nearestEntityUid = null;

        foreach (var lookupUid in _lookup.GetEntitiesInRange(position, 5f))
        {
            if (!_tileQuery.HasComponent(lookupUid))
                continue;
            var tileCords = Transform(lookupUid).Coordinates;
            var distance = Vector2.Distance(position.Position, tileCords.Position);

            if (!(distance < nearestDistance))
                continue;
            nearestDistance = distance;
            nearestEntityUid = lookupUid;
        }

        return (nearestEntityUid, nearestDistance);
    }

    private void OnSplitCore(EntityUid uid, BlobCoreComponent blobCoreComponent,
        BlobSplitCoreActionEvent args)
    {
        if (args.Handled)
            return;

        if (!blobCoreComponent.CanSplit)
        {
            _popup.PopupEntity(Loc.GetString("blob-cant-split"), args.Performer, args.Performer, PopupType.Large);
            return;
        }

        var gridUid = args.Target.GetGridUid(EntityManager);

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
        {
            return;
        }
        var centerTile = _mapSystem.GetLocalTilesIntersecting(gridUid.Value, grid,
            new Box2(args.Target.Position, args.Target.Position)).ToArray();

        EntityUid? blobTile = null;

        foreach (var tileref in centerTile)
        {
            foreach (var ent in _mapSystem.GetAnchoredEntities(gridUid.Value, grid,tileref.GridIndices))
            {
                if (!_tileQuery.HasComponent(ent))
                    continue;
                blobTile = ent;
                break;
            }
        }

        if (blobTile == null || !TryComp<BlobNodeComponent>(blobTile, out var blobNodeComponent))
        {
            _popup.PopupEntity(Loc.GetString("blob-target-node-blob-invalid"), args.Performer, args.Performer, PopupType.Large);
            args.Handled = true;
            return;
        }

        if (!_blobCoreSystem.TryUseAbility(args.Performer, uid, blobCoreComponent,
                blobCoreComponent.SplitCoreCost))
        {
            args.Handled = true;
            return;
        }

        QueueDel(blobTile.Value);
        var newCore = EntityManager.SpawnEntity(blobCoreComponent.CoreBlobTile, args.Target);
        blobCoreComponent.CanSplit = false;
        if (TryComp<BlobCoreComponent>(newCore, out var newBlobCoreComponent))
            newBlobCoreComponent.CanSplit = false;

        args.Handled = true;
    }


    private void OnSwapCore(EntityUid uid, BlobCoreComponent blobCoreComponent,
        BlobSwapCoreActionEvent args)
    {
        if (args.Handled)
            return;

        var gridUid = args.Target.GetGridUid(EntityManager);

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
        {
            return;
        }

        var centerTile = _mapSystem.GetLocalTilesIntersecting(gridUid.Value, grid,
            new Box2(args.Target.Position, args.Target.Position)).ToArray();

        EntityUid? blobTile = null;

        foreach (var tileRef in centerTile)
        {
            foreach (var ent in _mapSystem.GetAnchoredEntities(gridUid.Value, grid, tileRef.GridIndices))
            {
                if (!_tileQuery.HasComponent(ent))
                    continue;
                blobTile = ent;
                break;
            }
        }

        if (blobTile == null || !HasComp<BlobNodeComponent>(blobTile))
        {
            _popup.PopupEntity(Loc.GetString("blob-target-node-blob-invalid"), args.Performer, args.Performer, PopupType.Large);
            args.Handled = true;
            return;
        }

        if (!_blobCoreSystem.TryUseAbility(args.Performer, uid, blobCoreComponent,
                blobCoreComponent.SwapCoreCost))
        {
            args.Handled = true;
            return;
        }

        var nodePos = Transform(blobTile.Value).Coordinates;
        var corePos = Transform(uid).Coordinates;
        _transform.SetCoordinates(uid, nodePos.SnapToGrid());
        _transform.SetCoordinates(blobTile.Value, corePos.SnapToGrid());
        var xformCore = Transform(uid);
        if (!xformCore.Anchored)
        {
            _transform.AnchorEntity(uid, xformCore);
        }
        var xformNode = Transform(blobTile.Value);
        if (!xformNode.Anchored)
        {
            _transform.AnchorEntity(blobTile.Value, xformNode);
        }
        args.Handled = true;
    }

    private void OnBlobToNode(EntityUid uid, BlobCoreComponent blobCoreComponent,
        BlobToNodeActionEvent args)
    {
        if (args.Handled)
            return;

        var blobNodes = new List<EntityUid>();

        var blobNodeQuery = EntityQueryEnumerator<BlobNodeComponent, BlobTileComponent>();
        while (blobNodeQuery.MoveNext(out var ent, out var node, out var tile))
        {
            if (tile.Core == uid && !HasComp<BlobCoreComponent>(ent))
                blobNodes.Add(ent);
        }

        if (blobNodes.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("blob-not-have-nodes"), args.Performer, args.Performer, PopupType.Large);
            args.Handled = true;
            return;
        }

        _transform.SetCoordinates(args.Performer, Transform(_random.Pick(blobNodes)).Coordinates);
        args.Handled = true;
    }

    private void OnCreateBlobbernaut(EntityUid uid, BlobCoreComponent blobCoreComponent,
        BlobCreateBlobbernautActionEvent args)
    {
        if (args.Handled)
            return;

        var gridUid = args.Target.GetGridUid(EntityManager);

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
        {
            return;
        }

        var centerTile = _mapSystem.GetLocalTilesIntersecting(gridUid.Value, grid,
            new Box2(args.Target.Position, args.Target.Position)).ToArray();

        EntityUid? blobTile = null;

        foreach (var tileRef in centerTile)
        {
            foreach (var ent in _mapSystem.GetAnchoredEntities(gridUid.Value, grid, tileRef.GridIndices))
            {
                if (!HasComp<BlobFactoryComponent>(ent))
                    continue;
                blobTile = ent;
                break;
            }
        }

        if (blobTile == null || !TryComp<BlobFactoryComponent>(blobTile, out var blobFactoryComponent))
        {
            _popup.PopupEntity(Loc.GetString("blob-target-factory-blob-invalid"), args.Performer, args.Performer, PopupType.LargeCaution);
            return;
        }

        if (blobFactoryComponent.Blobbernaut != null)
        {
            _popup.PopupEntity(Loc.GetString("blob-target-already-produce-blobbernaut"), args.Performer, args.Performer, PopupType.LargeCaution);
            return;
        }

        if (!_blobCoreSystem.TryUseAbility(args.Performer, uid, blobCoreComponent, blobCoreComponent.BlobbernautCost))
            return;

        var ev = new ProduceBlobbernautEvent();
        RaiseLocalEvent(blobTile.Value, ev);

        _popup.PopupEntity(Loc.GetString("blob-spent-resource", ("point", blobCoreComponent.BlobbernautCost)),
            blobTile.Value,
            uid,
            PopupType.LargeCaution);

        args.Handled = true;
    }

    private void OnBlobToCore(EntityUid uid, BlobCoreComponent blobCoreComponent,
        BlobToCoreActionEvent args)
    {
        if (args.Handled)
            return;

        _transform.SetCoordinates(args.Performer, Transform(uid).Coordinates);
    }

    private void OnCreateNode(EntityUid uid, BlobCoreComponent blobCoreComponent,
        BlobCreateNodeActionEvent args)
    {
        if (args.Handled)
            return;

        var gridUid = args.Target.GetGridUid(EntityManager);

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
        {
            return;
        }

        var centerTile = _mapSystem.GetLocalTilesIntersecting(gridUid.Value, grid,
            new Box2(args.Target.Position, args.Target.Position)).ToArray();

        var blobTileType = BlobTileType.None;
        EntityUid? blobTile = null;

        foreach (var tileRef in centerTile)
        {
            foreach (var ent in _mapSystem.GetAnchoredEntities(gridUid.Value, grid, tileRef.GridIndices))
            {
                if (!_tileQuery.TryGetComponent(ent, out var blobTileComponent))
                    continue;
                blobTileType = blobTileComponent.BlobTileType;
                blobTile = ent;
                break;
            }
        }

        if (blobTileType is not BlobTileType.Normal ||
            blobTile == null)
        {
            _popup.PopupEntity(Loc.GetString("blob-target-normal-blob-invalid"), args.Performer, args.Performer, PopupType.Large);
            return;
        }

        var xform = Transform(blobTile.Value);

        var localPos = xform.Coordinates.Position;

        var radius = blobCoreComponent.NodeRadiusLimit;

        var innerTiles = _mapSystem.GetLocalTilesIntersecting(gridUid.Value, grid,
            new Box2(localPos + new Vector2(-radius, -radius), localPos + new Vector2(radius, radius)), false).ToArray();

        foreach (var tileRef in innerTiles)
        {
            foreach (var ent in _mapSystem.GetAnchoredEntities(gridUid.Value, grid, tileRef.GridIndices))
            {
                if (!HasComp<BlobNodeComponent>(ent))
                    continue;
                _popup.PopupEntity(Loc.GetString("blob-target-close-to-node"), args.Performer, args.Performer, PopupType.Large);
                return;
            }
        }

        if (!_blobCoreSystem.TryUseAbility(args.Performer, uid, blobCoreComponent, blobCoreComponent.NodeBlobCost))
            return;

        if (!_blobCoreSystem.TransformBlobTile(blobTile.Value,
                uid,
                blobCoreComponent.NodeBlobTile,
                args.Target,
                blobCoreComponent,
                transformCost: blobCoreComponent.NodeBlobCost))
            return;

        args.Handled = true;
    }

    private void OnCreateResource(EntityUid uid, BlobCoreComponent blobCoreComponent,
        BlobCreateResourceActionEvent args)
    {
        if (args.Handled)
            return;

        var gridUid = args.Target.GetGridUid(EntityManager);

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
        {
            return;
        }

        var centerTile = _mapSystem.GetLocalTilesIntersecting(gridUid.Value, grid,
            new Box2(args.Target.Position, args.Target.Position)).ToArray();

        var blobTileType = BlobTileType.None;
        EntityUid? blobTile = null;

        foreach (var tileref in centerTile)
        {
            foreach (var ent in _mapSystem.GetAnchoredEntities(gridUid.Value, grid, tileref.GridIndices))
            {
                if (!_tileQuery.TryGetComponent(ent, out var blobTileComponent))
                    continue;
                blobTileType = blobTileComponent.BlobTileType;
                blobTile = ent;
                break;
            }
        }

        if (blobTileType is not BlobTileType.Normal ||
            blobTile == null)
        {
            _popup.PopupEntity(Loc.GetString("blob-target-normal-blob-invalid"), args.Performer, args.Performer, PopupType.Large);
            return;
        }

        var xform = Transform(blobTile.Value);

        var localPos = xform.Coordinates.Position;

        var radius = blobCoreComponent.ResourceRadiusLimit;

        var innerTiles = _mapSystem.GetLocalTilesIntersecting(gridUid.Value, grid,
            new Box2(localPos + new Vector2(-radius, -radius), localPos + new Vector2(radius, radius)), false).ToArray();

        foreach (var tileRef in innerTiles)
        {
            foreach (var ent in _mapSystem.GetAnchoredEntities(gridUid.Value, grid, tileRef.GridIndices))
            {
                if (!HasComp<BlobResourceComponent>(ent) || HasComp<BlobCoreComponent>(ent))
                    continue;
                _popup.PopupEntity(Loc.GetString("blob-target-close-to-resource"), args.Performer, args.Performer, PopupType.Large);
                return;
            }
        }

        if (!_blobCoreSystem.CheckNearNode(args.Performer, xform.Coordinates, (gridUid.Value,grid), blobCoreComponent))
            return;

        if (!_blobCoreSystem.TryUseAbility(args.Performer,
                uid,
                blobCoreComponent,
                blobCoreComponent.ResourceBlobCost))
            return;

        if (!_blobCoreSystem.TransformBlobTile(blobTile.Value,
                uid,
                blobCoreComponent.ResourceBlobTile,
                args.Target,
                blobCoreComponent,
                transformCost: blobCoreComponent.ResourceBlobCost))
            return;

        args.Handled = true;
    }

    private void OnCreateFactory(EntityUid uid, BlobCoreComponent blobCoreComponent, BlobCreateFactoryActionEvent args)
    {
        if (args.Handled)
            return;

        var gridUid = args.Target.GetGridUid(EntityManager);

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
        {
            return;
        }

        var centerTile = _mapSystem.GetLocalTilesIntersecting(gridUid.Value, grid,
            new Box2(args.Target.Position, args.Target.Position)).ToArray();

        var blobTileType = BlobTileType.None;
        EntityUid? blobTile = null;

        foreach (var tileRef in centerTile)
        {
            foreach (var ent in _mapSystem.GetAnchoredEntities(gridUid.Value, grid, tileRef.GridIndices))
            {
                if (!_tileQuery.TryGetComponent(ent, out var blobTileComponent))
                    continue;
                blobTileType = blobTileComponent.BlobTileType;
                blobTile = ent;
                break;
            }
        }

        if (blobTileType is not BlobTileType.Normal ||
            blobTile == null)
        {
            _popup.PopupEntity(Loc.GetString("blob-target-normal-blob-invalid"), args.Performer, args.Performer, PopupType.Large);
            return;
        }

        var xform = Transform(blobTile.Value);

        var localPos = xform.Coordinates.Position;

        var radius = blobCoreComponent.FactoryRadiusLimit;

        var innerTiles = _mapSystem.GetLocalTilesIntersecting(gridUid.Value, grid,
            new Box2(localPos + new Vector2(-radius, -radius), localPos + new Vector2(radius, radius)), false).ToArray();

        foreach (var tileRef in innerTiles)
        {
            foreach (var ent in _mapSystem.GetAnchoredEntities(gridUid.Value, grid, tileRef.GridIndices))
            {
                if (!HasComp<BlobFactoryComponent>(ent))
                    continue;
                _popup.PopupEntity(Loc.GetString("Слишком близко к другой фабрике"), args.Performer, args.Performer, PopupType.Large);
                return;
            }
        }

        if (!_blobCoreSystem.CheckNearNode(args.Performer, xform.Coordinates, (gridUid.Value,grid), blobCoreComponent))
            return;

        if (!_blobCoreSystem.TryUseAbility(args.Performer, uid, blobCoreComponent,
                blobCoreComponent.FactoryBlobCost))
        {
            args.Handled = true;
            return;
        }

        if (!_blobCoreSystem.TransformBlobTile(null,
                uid,
                blobCoreComponent.FactoryBlobTile,
                args.Target,
                blobCoreComponent,
                transformCost: blobCoreComponent.FactoryBlobCost))
            return;

        args.Handled = true;
    }
}
