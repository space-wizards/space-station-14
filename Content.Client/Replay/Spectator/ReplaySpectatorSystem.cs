using System.Linq;
using Content.Client.Replay.UI;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Verbs;
using Robust.Client;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.Replays.Playback;
using Robust.Client.State;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Client.Replay.Spectator;

/// <summary>
/// This system handles spawning replay observer ghosts and maintaining their positions when traveling through time.
/// It also blocks most normal interactions, just in case.
/// </summary>
/// <remarks>
/// E.g., if an observer is on a grid, and then jumps forward or backward in time to a point where the grid does not
/// exist, where should the observer go? This attempts to maintain their position and eye rotation or just re-spawns
/// them as needed.
/// </remarks>
public sealed partial class ReplaySpectatorSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IConsoleHost _conHost = default!;
    [Dependency] private readonly IStateManager _stateMan = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly SharedContentEyeSystem _eye = default!;
    [Dependency] private readonly IReplayPlaybackManager _replayPlayback = default!;

    private ObserverData? _oldPosition;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerbs);
        SubscribeLocalEvent<ReplaySpectatorComponent, EntityTerminatingEvent>(OnTerminating);
        SubscribeLocalEvent<ReplaySpectatorComponent, PlayerDetachedEvent>(OnDetached);

        InitializeBlockers();
        _conHost.RegisterCommand("observe", ObserveCommand);

        _replayPlayback.BeforeSetTick += OnBeforeSetTick;
        _replayPlayback.AfterSetTick += OnAfterSetTick;
        _replayPlayback.ReplayPlaybackStarted += OnPlaybackStarted;
        _replayPlayback.ReplayPlaybackStopped += OnPlaybackStopped;
    }

    private void OnPlaybackStarted()
    {
        InitializeMovement();
        SetObserverPosition(default);
    }

    private void OnAfterSetTick()
    {
        if (_oldPosition != null)
            SetObserverPosition(_oldPosition.Value);
        _oldPosition = null;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _conHost.UnregisterCommand("observe");
        _replayPlayback.BeforeSetTick -= OnBeforeSetTick;
        _replayPlayback.AfterSetTick -= OnAfterSetTick;
        _replayPlayback.ReplayPlaybackStarted -= OnPlaybackStarted;
        _replayPlayback.ReplayPlaybackStopped -= OnPlaybackStopped;
    }

    private void OnPlaybackStopped()
    {
        ShutdownMovement();
    }

    private void OnBeforeSetTick()
    {
        _oldPosition = GetObserverPosition();
    }

    private void OnDetached(EntityUid uid, ReplaySpectatorComponent component, PlayerDetachedEvent args)
    {
        if (uid.IsClientSide())
            QueueDel(uid);
        else
            RemCompDeferred(uid, component);
    }

    public void SetObserverPosition(ObserverData observer)
    {
        if (Exists(observer.Entity) && Transform(observer.Entity).MapID != MapId.Nullspace)
        {
            _player.LocalPlayer!.AttachEntity(observer.Entity, EntityManager, _client);
            return;
        }

        if (observer.Local != null && observer.Local.Value.Coords.IsValid(EntityManager))
        {
            var newXform = SpawnObserverGhost(observer.Local.Value.Coords, false);
            newXform.LocalRotation = observer.Local.Value.Rot;
        }
        else if (observer.World != null && observer.World.Value.Coords.IsValid(EntityManager))
        {
            var newXform = SpawnObserverGhost(observer.World.Value.Coords, true);
            newXform.LocalRotation = observer.World.Value.Rot;
        }
        else if (TryFindFallbackSpawn(out var coords))
        {
            var newXform = SpawnObserverGhost(coords, true);
            newXform.LocalRotation = 0;
        }
        else
        {
            Logger.Error("Failed to find a suitable observer spawn point");
            return;
        }

        if (observer.Eye != null && TryComp(_player.LocalPlayer?.ControlledEntity, out InputMoverComponent? newMover))
        {
            newMover.RelativeEntity = observer.Eye.Value.Ent;
            newMover.TargetRelativeRotation = newMover.RelativeRotation = observer.Eye.Value.Rot;
        }
    }

    private bool TryFindFallbackSpawn(out EntityCoordinates coords)
    {
        var uid = EntityQuery<MapGridComponent>().OrderByDescending(x => x.LocalAABB.Size.LengthSquared).FirstOrDefault()?.Owner;
        coords = new EntityCoordinates(uid ?? default, default);
        return uid != null;
    }

    public struct ObserverData
    {
        // TODO REPLAYS handle ghost-following.
        public EntityUid Entity;
        public (EntityCoordinates Coords, Angle Rot)? Local;
        public (EntityCoordinates Coords, Angle Rot)? World;
        public (EntityUid? Ent, Angle Rot)? Eye;
    }

    public ObserverData GetObserverPosition()
    {
        var obs = new ObserverData();
        if (_player.LocalPlayer?.ControlledEntity is { } player && TryComp(player, out TransformComponent? xform) && xform.MapUid != null)
        {
            obs.Local = (xform.Coordinates, xform.LocalRotation);
            obs.World = (new(xform.MapUid.Value, xform.WorldPosition), xform.WorldRotation);

            if (TryComp(player, out InputMoverComponent? mover))
                obs.Eye = (mover.RelativeEntity, mover.TargetRelativeRotation);

            obs.Entity = player;
        }

        return obs;
    }

    private void OnTerminating(EntityUid uid, ReplaySpectatorComponent component, ref EntityTerminatingEvent args)
    {
        if (uid != _player.LocalPlayer?.ControlledEntity)
            return;

        var xform = Transform(uid);
        if (xform.MapUid == null || Terminating(xform.MapUid.Value))
            return;

        SpawnObserverGhost(new EntityCoordinates(xform.MapUid.Value, default), true);
    }

    private void OnGetAlternativeVerbs(GetVerbsEvent<AlternativeVerb> ev)
    {
        if (_replayPlayback.Replay == null)
            return;

        var verb = new AlternativeVerb
        {
            Priority = 100,
            Act = () =>
            {
                SpectateEntity(ev.Target);
            },

            Text = "Observe",
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/vv.svg.192dpi.png"))
        };

        ev.Verbs.Add(verb);
    }

    public void SpectateEntity(EntityUid target)
    {
        if (_player.LocalPlayer == null)
            return;

        var old = _player.LocalPlayer.ControlledEntity;

        if (old == target)
        {
            // un-visit
            SpawnObserverGhost(Transform(target).Coordinates, true);
            return;
        }

        _player.LocalPlayer.AttachEntity(target, EntityManager, _client);
        EnsureComp<ReplaySpectatorComponent>(target);

        if (old == null)
            return;

        if (old.Value.IsClientSide())
            Del(old.Value);
        else
            RemComp<ReplaySpectatorComponent>(old.Value);

        _stateMan.RequestStateChange<ReplaySpectateEntityState>();
    }

    public TransformComponent SpawnObserverGhost(EntityCoordinates coords, bool gridAttach)
    {
        if (_player.LocalPlayer == null)
            throw new InvalidOperationException();

        var old = _player.LocalPlayer.ControlledEntity;

        var ent = Spawn("MobObserver", coords);
        _eye.SetMaxZoom(ent, Vector2.One * 5);
        EnsureComp<ReplaySpectatorComponent>(ent);

        var xform = Transform(ent);

        if (gridAttach)
            _transform.AttachToGridOrMap(ent);

        _player.LocalPlayer.AttachEntity(ent, EntityManager, _client);

        if (old != null)
        {
            if (old.Value.IsClientSide())
                QueueDel(old.Value);
            else
                RemComp<ReplaySpectatorComponent>(old.Value);
        }

        _stateMan.RequestStateChange<ReplayGhostState>();

        return xform;
    }

    private void ObserveCommand(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            if (_player.LocalPlayer?.ControlledEntity is { } current)
                SpawnObserverGhost(new EntityCoordinates(current, default), true);
            return;
        }

        if (!EntityUid.TryParse(args[0], out var uid))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-uid", ("arg", args[0])));
            return;
        }

        if (!TryComp(uid, out TransformComponent? xform))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-entity-exist", ("arg", args[0])));
            return;
        }

        SpectateEntity(uid);
    }
}
