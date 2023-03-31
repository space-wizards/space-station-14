using Content.Server.Mind.Components;
using Content.Server.Physics.Controllers;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.Medical.Surgery;
using Content.Shared.Movement.Events;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Medical.Surgery;

public sealed class SurgeryRealmSystem : EntitySystem
{
    private const int SectionSeparation = 100;

    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IMapManager _maps = default!;

    [Dependency] private readonly MoverController _mover = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;

    private MapId _surgeryRealmMap = MapId.Nullspace;
    private int _sections;

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

        SubscribeLocalEvent<InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SurgeryRealmHeartComponent, CanWeightlessMoveEvent>(OnHeartCanWeightlessMove);
    }

    private void OnHeartCanWeightlessMove(EntityUid uid, SurgeryRealmHeartComponent component, ref CanWeightlessMoveEvent args)
    {
        args.CanMove = true;
    }

    private void OnInteractUsing(InteractUsingEvent args)
    {
        if (!HasComp<SurgeryRealmToolComponent>(args.Used))
        {
            return;
        }

        if (!HasComp<SurgeryRealmVictimComponent>(args.User) &&
            TryComp(args.User, out ActorComponent? userActor))
        {
            StartOperation(userActor.PlayerSession, args.Used);
        }


        if (args.User != args.Target)
        {
            if (HasComp<SurgeryRealmVictimComponent>(args.Target) ||
                !TryComp(args.Target, out ActorComponent? targetActor))
            {
                return;
            }

            StartOperation(targetActor.PlayerSession, args.Used);
        }
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        if (_surgeryRealmMap == MapId.Nullspace || !_maps.MapExists(_surgeryRealmMap))
            return;

        _maps.DeleteMap(_surgeryRealmMap);
        _surgeryRealmMap = MapId.Nullspace;
        _sections = 0;
    }

    private void StartOperation(IPlayerSession victimPlayer, EntityUid toolId)
    {
        if (victimPlayer.AttachedEntity is not { } victimEntity)
            return;

        var tool = EnsureComp<SurgeryRealmToolComponent>(toolId);
        var victim = EnsureComp<SurgeryRealmVictimComponent>(victimEntity);

        EnsureMap();

        if (tool.Position == null || tool.Victims.Count == 0)
            tool.Position = new MapCoordinates(GetNextPosition(), _surgeryRealmMap);

        tool.Victims.Add(victimEntity);

        victim.Heart = Spawn(tool.HeartPrototype, tool.Position.Value.Offset(0, -5));
        _console.ExecuteCommand($"scale {victim.Heart} 2.5");

        var clown = Spawn("SurgeryRealmClown", tool.Position.Value.Offset(0, 3));
        _console.ExecuteCommand($"scale {clown} 5");

        SpawnEdges(tool.Position.Value);

        var camera = CreateCamera(victimPlayer, tool.Position.Value);

        _mover.SetRelay(camera, victim.Heart);

        var mind = EnsureComp<MindComponent>(victimEntity);
        mind.Mind?.Visit(camera);

        RaiseNetworkEvent(new SurgeryRealmStartEvent(camera));
    }

    private void SpawnEdges(MapCoordinates coordinates)
    {
        for (var x = -5; x < 6; x++)
        {
            Spawn("SurgeryRealmEdge", coordinates.Offset(x, -1));
            Spawn("SurgeryRealmEdge", coordinates.Offset(x, -7));
        }

        for (var y = -7; y < 0; y++)
        {
            Spawn("SurgeryRealmEdge", coordinates.Offset(6, y));
            Spawn("SurgeryRealmEdge", coordinates.Offset(-6, y));
        }
    }

    private void StopOperation(IPlayerSession victim, EntityUid toolId)
    {
        if (victim.AttachedEntity is not { } victimEntity ||
            !TryComp(toolId, out SurgeryRealmToolComponent? tool))
        {
            return;
        }

        tool.Victims.Remove(victimEntity);
    }

    private void EnsureMap()
    {
        if (_surgeryRealmMap != MapId.Nullspace && _maps.MapExists(_surgeryRealmMap))
            return;

        _surgeryRealmMap = _maps.CreateMap();
        var map = Comp<MapComponent>(_maps.GetMapEntityId(_surgeryRealmMap));

        map.LightingEnabled = false;
        Dirty(map);
    }

    // Copied from TabletopSystem
    private Vector2 GetNextPosition()
    {
        return UlamSpiral(_sections++) * SectionSeparation;
    }

    private Vector2i UlamSpiral(int n)
    {
        var k = (int)MathF.Ceiling(MathF.Sqrt(n) - 1) / 2;
        var t = 2 * k + 1;
        var m = (int)MathF.Pow(t, 2);
        t--;

        if (n >= m - t)
            return new Vector2i(k - (m - n), -k);

        m -= t;

        if (n >= m - t)
            return new Vector2i(-k, -k + (m - n));

        m -= t;

        if (n >= m - t)
            return new Vector2i(-k + (m - n), k);

        return new Vector2i(k, k - (m - n - t));
    }

    private EntityUid CreateCamera(IPlayerSession player, MapCoordinates position)
    {
        var camera = EntityManager.SpawnEntity("SurgeryRealmCamera", position);

        var eyeComponent = EnsureComp<EyeComponent>(camera);
        eyeComponent.DrawFov = false;
        _viewSubscriber.AddViewSubscriber(camera, player);

        return camera;
    }
}
