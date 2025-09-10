using Content.Server.Actions;
using Content.Server.Station.Systems;
using Content.Shared.Eye;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Pinpointer;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Interaction.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.UserInterface;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Server.GameObjects;
using Content.Shared._Starlight.Computers.RemoteEye;
using System.Linq;
using Content.Shared.Station.Components;
using Content.Shared._Starlight.Action;
using Content.Shared.Whitelist;
using Robust.Shared.Player;

namespace Content.Server._Starlight.Computers.RemoteEye;

public sealed partial class RemoteEyeSystem : SharedRemoteEyeSystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly StarlightActionsSystem _slActions = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RemoteEyeConsoleComponent, ActivatableUIOpenAttemptEvent>(OnActivatableUIOpenAttemptEvent);
        SubscribeLocalEvent<RemoteEyeConsoleComponent, BeforeActivatableUIOpenEvent>(OnBeforeActivatableUIOpen);
        SubscribeLocalEvent<RemoteEyeActorComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<RemoteEyeActorComponent, GetVisMaskEvent>(OnGetVisMask);
        SubscribeLocalEvent<ExitConsoleEvent>(OnExit);

        Subs.BuiEvents<RemoteEyeConsoleComponent>(RemoteEyeUIKey.Key, subs => subs.Event<BeaconChosenBuiMsg>(OnBeaconChosenBuiMsg));
        base.Initialize();
    }

    public void CameraExit(EntityUid actor)
    {
        if (!TryComp<RelayInputMoverComponent>(actor, out var comp)) 
            return;

        var relay = comp.RelayEntity;
        RemComp(actor, comp);

        RemoveActions(actor, out var remoteEyeActor);
        if (remoteEyeActor.VirtualItem.HasValue)
            _virtualItem.DeleteInHandsMatching(actor, remoteEyeActor.VirtualItem.Value);

        RemComp<StationAiOverlayComponent>(actor);

        _eye.SetTarget(actor, null);
        _eye.RefreshVisibilityMask(actor);
        _eye.SetDrawFov(actor, true);

        QueueDel(relay);
    }

    private void OnActivatableUIOpenAttemptEvent(Entity<RemoteEyeConsoleComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (ent.Comp.Whitelist != null && _whitelistSystem.IsWhitelistFail(ent.Comp.Whitelist, args.User))
            args.Cancel();
    }

    private void OnBeforeActivatableUIOpen(Entity<RemoteEyeConsoleComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        var stations = _stationSystem.GetStations();
        var result = new Dictionary<int, StationBeacons>();

        foreach (var station in stations)
        {
            var stationEnt = (station, Comp<StationDataComponent>(station));

            if (_stationSystem.GetLargestGrid(stationEnt) is not { } grid
                || !TryComp(station, out MetaDataComponent? stationMetaData)
                || !_entityManager.TryGetComponent<NavMapComponent>(grid, out var navMap))
                continue;

            result.Add(station.Id, new StationBeacons
            {
                Name = stationMetaData.EntityName,
                StationId = station.Id,
                Beacons = [.. navMap.Beacons.Values],
            });
        }

        _uiSystem.SetUiState(ent.Owner, RemoteEyeUIKey.Key, new RemoteEyeConsoleBuiState() { Stations = result, Color = ent.Comp.Color });
    }

    private void OnBeaconChosenBuiMsg(Entity<RemoteEyeConsoleComponent> ent, ref BeaconChosenBuiMsg args)
    {
        CameraExit(args.Actor);

        var beacon = _entityManager.GetEntity(args.Beacon.NetEnt);
        var eye = SpawnAtPosition(ent.Comp.RemoteEntityProto, Transform(beacon).Coordinates);
        ent.Comp.RemoteEntity = eye;

        var remoteEyeActor = EnsureComp<RemoteEyeActorComponent>(args.Actor);
        remoteEyeActor.VirtualItem = ent.Owner;
        remoteEyeActor.RemoteEntity = eye;

        if (TryComp<HandsComponent>(args.Actor, out var handsComponent))
        {
            var handy = (args.Actor, handsComponent);
            foreach (var hand in _hands.EnumerateHands(handy))
            {
                if (_hands.GetHeldItem(handy, hand) is var item)
                {
                    if (HasComp<UnremoveableComponent>(item))
                        continue;

                    _hands.DoDrop(handy, hand, true);
                }

                if (_virtualItem.TrySpawnVirtualItemInHand(ent.Owner, args.Actor, out var virtItem))
                    EnsureComp<UnremoveableComponent>(virtItem.Value);
            }
        }

        if (TryComp(args.Actor, out EyeComponent? eyeComp))
        {
            _eye.RefreshVisibilityMask(args.Actor);
            _eye.SetTarget(args.Actor, eye, eyeComp);
            _eye.SetDrawFov(args.Actor, false);

            if (TryComp<StationAiOverlayComponent>(eye, out var eyeOverlay))
                AddComp(args.Actor, new StationAiOverlayComponent
                {
                    AllowCrossGrid = eyeOverlay.AllowCrossGrid,
                    Alfa = eyeOverlay.Alfa
                });

            if (!TryComp(eye, out RemoteEyeSourceContainerComponent? remoteEyeSourceContainerComponent))
            {
                remoteEyeSourceContainerComponent = new RemoteEyeSourceContainerComponent { Actor = args.Actor };
                AddComp(eye, remoteEyeSourceContainerComponent);
            }
            else
                remoteEyeSourceContainerComponent.Actor = args.Actor;

            Dirty(eye, remoteEyeSourceContainerComponent);
        }

        AddActions(ent, (args.Actor, remoteEyeActor));
        Dirty(ent);

        _mover.SetRelay(args.Actor, eye);
    }
    private void OnPlayerAttached(Entity<RemoteEyeActorComponent> ent, ref PlayerAttachedEvent args) 
        => _eye.RefreshVisibilityMask((ent.Owner, null));

    private void OnGetVisMask(Entity<RemoteEyeActorComponent> ent, ref GetVisMaskEvent args)
    {
        if (ent.Comp.RemoteEntity is not { Valid: true } eye
            || !TryComp<VisibilityComponent>(eye, out var visibility))
            return;

        args.VisibilityMask |= visibility.Layer;
    }

    private void OnExit(ExitConsoleEvent ev) => CameraExit(ev.Performer);

    private void AddActions(Entity<RemoteEyeConsoleComponent> ent, Entity<RemoteEyeActorComponent> performer)
    {
        performer.Comp.HiddenActions = _slActions.HideActions(performer);

        performer.Comp.ActionsEntities = new EntityUid?[ent.Comp.Actions.Length];
        for (var i = 0; i < ent.Comp.Actions.Length; i++)
            _actions.AddAction(performer, ref performer.Comp.ActionsEntities[i], ent.Comp.Actions[i]);
    }

    private void RemoveActions(EntityUid actor, out RemoteEyeActorComponent comp)
    {
        EnsureComp(actor, out comp);

        foreach (var actionEnt in comp.ActionsEntities.Where(static x => x is not null and { Valid: true }))
            _actions.RemoveAction(actor, actionEnt);

        _slActions.UnHideActions(actor, comp.HiddenActions);
    }
}
