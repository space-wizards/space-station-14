using System.Numerics;
using Content.Client.Replay.UI;
using Content.Shared.Verbs;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Client.Replay.Spectator;

// This partial class has methods for spawning a spectator ghost and "possessing" entitites.
public sealed partial class ReplaySpectatorSystem
{
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

            Text = Loc.GetString("replay-verb-spectate"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/vv.svg.192dpi.png"))
        };

        ev.Verbs.Add(verb);
    }

    public void SpectateEntity(EntityUid target)
    {
        if (_player.LocalSession == null)
            return;

        var old = _player.LocalSession.AttachedEntity;

        if (old == target)
        {
            // un-visit
            SpawnSpectatorGhost(Transform(target).Coordinates, true);
            return;
        }

        _player.SetAttachedEntity(_player.LocalSession, target);
        EnsureComp<ReplaySpectatorComponent>(target);

        _stateMan.RequestStateChange<ReplaySpectateEntityState>();
        if (old == null)
            return;

        if (IsClientSide(old.Value))
            Del(old.Value);
        else
            RemComp<ReplaySpectatorComponent>(old.Value);
    }

    public TransformComponent SpawnSpectatorGhost(EntityCoordinates coords, bool gridAttach)
    {
        if (_player.LocalSession == null)
            throw new InvalidOperationException();

        var old = _player.LocalSession.AttachedEntity;

        var ent = Spawn("ReplayObserver", coords);
        _eye.SetMaxZoom(ent, Vector2.One * 5);
        EnsureComp<ReplaySpectatorComponent>(ent);

        var xform = Transform(ent);

        if (gridAttach)
            _transform.AttachToGridOrMap(ent);

        _player.SetAttachedEntity(_player.LocalSession, ent);

        if (old != null)
        {
            if (IsClientSide(old.Value))
                QueueDel(old.Value);
            else
                RemComp<ReplaySpectatorComponent>(old.Value);
        }

        _stateMan.RequestStateChange<ReplayGhostState>();

        _spectatorData = GetSpectatorData();
        return xform;
    }

    private void SpectateCommand(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            if (_player.LocalSession?.AttachedEntity is { } current)
                SpawnSpectatorGhost(new EntityCoordinates(current, default), true);
            else
                SpawnSpectatorGhost(default, true);
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netEntity))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-uid", ("arg", args[0])));
            return;
        }

        var uid = GetEntity(netEntity);

        if (!Exists(uid))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-entity-exist", ("arg", args[0])));
            return;
        }

        SpectateEntity(uid);
    }

    private CompletionResult SpectateCompletions(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        return CompletionResult.FromHintOptions(CompletionHelper.NetEntities(args[0],
            EntityManager), Loc.GetString("cmd-replay-spectate-hint"));
    }
}
