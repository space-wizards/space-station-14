using System.Threading;
using Content.Server._Craft.Utils;
using Content.Server.Cargo.Components;
using Content.Server.Chat.Systems;
using Content.Server.Construction.Components;
using Content.Server.GameTicking;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.UserInterface;
using Content.Shared.Cargo.Components;
using Content.Shared.Construction.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server._Craft.StationGoals.Scipts;

//Выключено в настройках события.
//Делаем его тестовым и включаем ручками, чтобы последить за правильностью чистки ресурсов
public sealed class SecretObjectResearch : IStationGoalScript
{
    private readonly string CargoShuttlePath = "/Maps/Shuttles/cargo.yml";
    private readonly string ArtifactSpawnerPrototype = "RandomArtifactSpawner";
    private readonly int MaxArtifacts = 7;
    private readonly int MinArtifacts = 4;
    private MapId MapId = MapId.Nullspace;
    private EntityUid ShuttleUid = EntityUid.Invalid;

    private CancellationTokenSource? token = null;
    public void PerformAction(StationGoalPrototype stationGoal, IPrototypeManager prototypeManager, EntitySystem entitySystem)
    {
        var mapManager = IoCManager.Resolve<IMapManager>();
        var entitySystemManager = IoCManager.Resolve<IEntitySystemManager>();
        var mapLoaderSystem = entitySystemManager.GetEntitySystem<MapLoaderSystem>();
        var shuttleSystem = entitySystemManager.GetEntitySystem<ShuttleSystem>();
        var gameTicker = entitySystemManager.GetEntitySystem<GameTicker>();
        var stationSystem = entitySystemManager.GetEntitySystem<StationSystem>();
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var chatSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ChatSystem>();


        (MapId, ShuttleUid) = ShuttleUtils.CreateShuttleOnNewMap(mapManager, mapLoaderSystem, entityManager, CargoShuttlePath);

        if (MapId == MapId.Nullspace || ShuttleUid == EntityUid.Invalid)
            return;

        var targetStation = ShuttleUtils.GetTargetStation(gameTicker, mapManager, entityManager);
        if (targetStation == EntityUid.Invalid)
        {
            return;
        }
        var targetGrid = stationSystem.GetLargestGrid(entityManager.GetComponent<StationDataComponent>(targetStation)).GetValueOrDefault();

        MakeShuttleUnDestroyable(mapManager, entityManager, entitySystemManager);
        SpawnArtifacts(entityManager);
        SetupObjectArrival(mapManager, entityManager, shuttleSystem, chatSystem, targetGrid);
    }

    private void SpawnArtifacts(IEntityManager entityManager)
    {
        var artifactsCount = IoCManager.Resolve<IRobustRandom>().Next(MinArtifacts, MaxArtifacts);
        var counter = 0;
        var pallets = GetCargoPallets(entityManager);

        foreach (var (comp, compXform) in entityManager.EntityQuery<CargoPalletComponent, TransformComponent>(true))
        {
            if (counter == artifactsCount) break;
            if (compXform.ParentUid != ShuttleUid || !compXform.Anchored) continue;

            entityManager.SpawnEntity(ArtifactSpawnerPrototype, compXform.Coordinates);
            counter++;
        }
    }

    private void MakeShuttleUnDestroyable(IMapManager mapManager, IEntityManager entityManager, IEntitySystemManager entitySystemManager)
    {
        var entityLookupSystem = entitySystemManager.GetEntitySystem<EntityLookupSystem>();
        var boxForEntityLookup = new Box2(new Vector2(-50, -50), new Vector2(50, 50));
        var entitiesToDisableDestroy = entityLookupSystem.GetEntitiesIntersecting(MapId, boxForEntityLookup);

        foreach (var entity in entitiesToDisableDestroy)
        {
            entityManager.RemoveComponent<ActivatableUIComponent>(entity);
            entityManager.RemoveComponent<ConstructionComponent>(entity);
            entityManager.RemoveComponent<AnchorableComponent>(entity);
        }
    }

    private List<CargoPalletComponent> GetCargoPallets(IEntityManager entityManager)
    {
        var pads = new List<CargoPalletComponent>();

        foreach (var (comp, compXform) in entityManager.EntityQuery<CargoPalletComponent, TransformComponent>(true))
        {
            if (compXform.ParentUid != ShuttleUid || !compXform.Anchored) continue;

            pads.Add(comp);
        }

        return pads;
    }

    private void SetupObjectArrival(IMapManager mapManager, IEntityManager entityManager, ShuttleSystem shuttleSystem, ChatSystem chatSystem, EntityUid target)
    {
        token = new CancellationTokenSource();
        Timer.Spawn(
            milliseconds: 120000,
            onFired: () =>
            {
                var shuttleComponent = entityManager.EnsureComponent<ShuttleComponent>(ShuttleUid);
                shuttleSystem.TryFTLDock(
                    component: shuttleComponent,
                    targetUid: target
                );

                ChatUtils.SendLocMessageFromCentcom(chatSystem, "station-goal-artifacts-research-incomming", null);
                ScheduleShuttleUnDock(mapManager, entityManager, shuttleSystem, chatSystem);
            },
            cancellationToken: token.Token
        );
    }

    private void ScheduleShuttleUnDockWarning(IMapManager mapManager, IEntityManager entityManager, ShuttleSystem shuttleSystem, ChatSystem chatSystem)
    {
        token = new CancellationTokenSource();
        Timer.Spawn(
            milliseconds: 600000,
            onFired: () =>
            {
                ChatUtils.SendLocMessageFromCentcom(chatSystem, "station-goal-artifacts-research-remove", null);
                ScheduleShuttleUnDock(mapManager, entityManager, shuttleSystem, chatSystem);
            },
            cancellationToken: token.Token
        );
    }

    private void ScheduleShuttleUnDock(IMapManager mapManager, IEntityManager entityManager, ShuttleSystem shuttleSystem, ChatSystem chatSystem)
    {
        token = new CancellationTokenSource();
        Timer.Spawn(
            milliseconds: 600000,
            onFired: () =>
            {
                var shuttleComponent = entityManager.EnsureComponent<ShuttleComponent>(ShuttleUid);
                shuttleSystem.FTLTravel(
                    component: shuttleComponent,
                    target: mapManager.GetMapEntityId(MapId),
                    startupTime: 30,
                    hyperspaceTime: 30,
                    dock: false
                );

                ScheduleShuttleDestroy();
            },
            cancellationToken: token.Token
        );
    }


    private void ScheduleShuttleDestroy()
    {
        token = new CancellationTokenSource();
        Timer.Spawn(
            milliseconds: 60000,
            onFired: () => Cleanup(),
            cancellationToken: token.Token
        );
    }
    public void Cleanup()
    {
        token?.Cancel();
        token = null;

        IoCManager.Resolve<IMapManager>().DeleteMap(MapId);
        IoCManager.Resolve<IEntityManager>().QueueDeleteEntity(ShuttleUid);

        MapId = MapId.Nullspace;
        ShuttleUid = EntityUid.Invalid;
    }
}
