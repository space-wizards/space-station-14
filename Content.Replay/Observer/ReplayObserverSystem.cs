using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.Actions.Widgets;
using Content.Client.UserInterface.Systems.Alerts.Widgets;
using Content.Client.UserInterface.Systems.Chat;
using Content.Client.UserInterface.Systems.Ghost.Widgets;
using Content.Client.UserInterface.Systems.Hotbar.Widgets;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Replay.UI;
using Content.Replay.UI.TimeWidget;
using Content.Shared.Movement.Components;
using Content.Shared.Verbs;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.LayoutContainer;

namespace Content.Replay.Observer;

public sealed partial class ReplayObserverSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IConsoleHost _conHost = default!;
    [Dependency] private readonly IUserInterfaceManager _uiMan = default!;
    [Dependency] private readonly IStateManager _stateMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerbs);
        SubscribeLocalEvent<ReplayObserverComponent, EntityTerminatingEvent>(OnTerminating);
        SubscribeLocalEvent<ReplayObserverComponent, PlayerDetachedEvent>(OnDetached);

        InitializeBlockers();
        _conHost.RegisterCommand("observe", ObserveCommand);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _conHost.UnregisterCommand("observe");
    }
    private void OnDetached(EntityUid uid, ReplayObserverComponent component, PlayerDetachedEvent args)
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
            _player.LocalPlayer!.AttachEntity(observer.Entity);
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

    private void OnTerminating(EntityUid uid, ReplayObserverComponent component, ref EntityTerminatingEvent args)
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
        var verb = new AlternativeVerb
        {
            Priority = 100,
            Act = (() =>
            {
                SpectateEntity(ev.Target);
            }),

            Text = "Observe",
            Icon = new SpriteSpecifier.Texture(new ResourcePath("/Textures/Interface/VerbIcons/vv.svg.192dpi.png"))
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

        _player.LocalPlayer.AttachEntity(target);
        EnsureComp<ReplayObserverComponent>(target);

        if (old == null)
            return;

        if (old.Value.IsClientSide())
            Del(old.Value);
        else
            RemComp<ReplayObserverComponent>(old.Value);

        _stateMan.RequestStateChange<ReplaySpectateEntityState>();
    }

    public TransformComponent SpawnObserverGhost(EntityCoordinates coords, bool gridAttach)
    {
        if (_player.LocalPlayer == null)
            throw new InvalidOperationException();

        var old = _player.LocalPlayer.ControlledEntity;

        var ent = Spawn("MobObserver", coords);
        EnsureComp<ReplayObserverComponent>(ent);
        var xform = Transform(ent);

        if (gridAttach)
            xform.AttachToGridOrMap();

        _player.LocalPlayer.AttachEntity(ent);

        if (old != null)
        {
            if (old.Value.IsClientSide())
                QueueDel(old.Value);
            else
                RemComp<ReplayObserverComponent>(old.Value);
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
