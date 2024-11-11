using Content.Server.Actions;
using Content.Server.DoAfter;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.Actions;
using Content.Shared.Eye;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Pinpointer;
using Content.Shared.Silicons.StationAi;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Content.Shared.Tag;
using Content.Server.DeviceLinking.Systems;
using Robust.Server.Containers;

namespace Content.Server.Starlight.Antags.Abductor;

public sealed partial class AbductorSystem : SharedAbductorSystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly ActionContainerSystem _actionsCont = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly TransformSystem _xformSys = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly ContainerSystem _container = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AbductorHumanObservationConsoleComponent, BeforeActivatableUIOpenEvent>(OnBeforeActivatableUIOpen);

        Subs.BuiEvents<AbductorHumanObservationConsoleComponent>(AbductorCameraConsoleUIKey.Key, subs => subs.Event<AbductorBeaconChosenBuiMsg>(OnAbductorBeaconChosenBuiMsg));
        InitializeActions();
        InitializeGizmo();
        InitializeConsole();
        InitializeOrgans();
        base.Initialize();
    }

    private void OnAbductorBeaconChosenBuiMsg(Entity<AbductorHumanObservationConsoleComponent> ent, ref AbductorBeaconChosenBuiMsg args)
    {
        OnCameraExit(ent.Owner);
        if (ent.Comp.RemoteEntityProto != null)
        {
            var beacon = _entityManager.GetEntity(args.Beacon.NetEnt);
            var eye = SpawnAtPosition(ent.Comp.RemoteEntityProto, Transform(beacon).Coordinates);
            ent.Comp.RemoteEntity = eye;

            var visibility = EnsureComp<VisibilityComponent>(eye);

            Dirty(ent);

            if (TryComp(args.Actor, out EyeComponent? eyeComp))
            {
                _eye.SetVisibilityMask(args.Actor, eyeComp.VisibilityMask | (int)VisibilityFlags.Abductor, eyeComp);
                _eye.SetTarget(args.Actor, ent.Comp.RemoteEntity.Value, eyeComp);
                _eye.SetDrawFov(args.Actor, false);

                if (!HasComp<StationAiOverlayComponent>(args.Actor))
                    AddComp(args.Actor, new StationAiOverlayComponent { AllowCrossGrid = true });
                if (!TryComp(ent.Comp.RemoteEntity, out RemoteEyeSourceContainerComponent? remoteEyeSourceContainerComponent))
                {
                    remoteEyeSourceContainerComponent = new RemoteEyeSourceContainerComponent { Actor = args.Actor };
                    AddComp(ent.Comp.RemoteEntity.Value, remoteEyeSourceContainerComponent);
                }
                else
                    remoteEyeSourceContainerComponent.Actor = args.Actor;
                Dirty(ent.Comp.RemoteEntity.Value, remoteEyeSourceContainerComponent);
            }

            AddActions(args);

            _mover.SetRelay(args.Actor, ent.Comp.RemoteEntity.Value);
        }
    }

    private void OnCameraExit(EntityUid actor)
    {
        if (TryComp<RelayInputMoverComponent>(actor, out var comp))
        {
            var relay = comp.RelayEntity;
            RemComp(actor, comp);

            if (TryComp(actor, out EyeComponent? eyeComp))
            {
                if (HasComp<StationAiOverlayComponent>(actor))
                    RemComp<StationAiOverlayComponent>(actor);

                _eye.SetVisibilityMask(actor, eyeComp.VisibilityMask ^ (int)VisibilityFlags.Abductor, eyeComp);
                _eye.SetDrawFov(actor, true);
            }
            RemoveActions(actor);
            QueueDel(relay);
        }
    }

    private void OnBeforeActivatableUIOpen(Entity<AbductorHumanObservationConsoleComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        var stations = _stationSystem.GetStations();
        var result = new Dictionary<int, StationBeacons>();

        foreach (var station in stations)
        {
            if (_stationSystem.GetLargestGrid(Comp<StationDataComponent>(station)) is not { } grid
                || !TryComp(station, out MetaDataComponent? stationMetaData))
                return;

            var mapId = Transform(grid).MapID;

            if (!_entityManager.TryGetComponent<NavMapComponent>(grid, out var navMap))
                return;

            result.Add(station.Id, new StationBeacons
            {
                Name = stationMetaData.EntityName,
                StationId = station.Id,
                Beacons = [.. navMap.Beacons.Values],
            });
        }

        _uiSystem.SetUiState(ent.Owner, AbductorCameraConsoleUIKey.Key, new AbductorCameraConsoleBuiState() { Stations = result });
    }
}
