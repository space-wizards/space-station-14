using Content.Server.Administration;
using Content.Server.EUI;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.UI;
using Content.Server.Mind.Commands;
using Content.Server.Mind.Components;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Robust.Server.GameObjects;
using Robust.Server.Placement;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Ghost.Roles;

/// <summary>
/// This handles...
/// </summary>
public sealed class GhostRoleGroupSystem : EntitySystem
{
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly GhostRoleManager _ghostRoleManager = default!;

    private readonly Dictionary<IPlayerSession, GhostRoleGroupsEui> _openUis = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
        SubscribeLocalEvent<GhostRoleComponent, EntityPlacedEvent>(OnEntityPlaced);
        SubscribeLocalEvent<GhostRoleGroupComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<GhostRoleGroupComponent, ComponentShutdown>(OnShutdown);

        _ghostRoleManager.OnGhostRoleGroupChanged += OnGhostRoleGroupChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _ghostRoleManager.OnGhostRoleGroupChanged -= OnGhostRoleGroupChanged;
    }

    private void OnGhostRoleGroupChanged(GhostRoleGroupChangedEventArgs ev)
    {
        UpdateAllEui();
    }

    private void OnMindAdded(EntityUid uid, GhostRoleGroupComponent component, MindAddedMessage args)
    {
        _ghostRoleManager.RemoveEntityFromGhostRoleGroup(component.Identifier, uid);
    }

    private void Reset(RoundRestartCleanupEvent ev)
    {
        foreach (var session in _openUis.Keys)
        {
            CloseEui(session);
        }

        _openUis.Clear();
    }

    public void OpenEui(IPlayerSession session)
    {
        if(_openUis.ContainsKey(session))
            CloseEui(session);

        var eui = _openUis[session] = new GhostRoleGroupsEui();
        _euiManager.OpenEui(eui, session);
        eui.StateDirty();
    }

    public void CloseEui(IPlayerSession session)
    {
        if(_openUis.Remove(session, out var eui))
            eui?.Close();
    }

    public void UpdateAllEui()
    {
        foreach (var eui in _openUis.Values)
        {
            eui.StateDirty();
        }
    }

    private void OnEntityPlaced(EntityUid uid, GhostRoleComponent component, EntityPlacedEvent args)
    {
        var identifier = _ghostRoleManager.GetActiveGhostRoleGroupOrNull(args.PlacedBy);
        if (identifier == null)
            return;

        Logger.Debug($"Added {ToPrettyString(args.Placed)} to role group.");
        MakeSentientCommand.MakeSentient(uid, EntityManager);

        var comp = AddComp<GhostRoleGroupComponent>(uid);
        comp.Identifier = identifier.Value;

        _ghostRoleManager.AttachToGhostRoleGroup(args.PlacedBy, identifier.Value, uid, component);
    }

    private void OnShutdown(EntityUid uid, GhostRoleGroupComponent role, ComponentShutdown args)
    {
        _ghostRoleManager.DetachFromGhostRoleGroup(role.Identifier, uid);
    }
}

[AdminCommand(AdminFlags.Spawn)]
public sealed class GhostRoleGroupsCommand : IConsoleCommand
{
    public string Command => "ghostrolegroups";
    public string Description => "Manage ghost role groups.";
    public string Help => @$"${Command}
start <name> <description> <rules>
delete <deleteEntities> <groupIdentifier>
release [groupIdentifier]
open";

    private void ExecuteStart(IConsoleShell shell, IPlayerSession player, string argStr, string[] args)
    {
        if (args.Length < 3)
            return;

        var manager = IoCManager.Resolve<GhostRoleManager>();

        var name = args[1];
        var description = args[2];

        var id = manager.StartGhostRoleGroup(player, name, description);
        shell.WriteLine($"Role group start: {id}");
    }

    private void ExecuteDelete(IConsoleShell shell, IPlayerSession player, string argStr, string[] args)
    {
        var manager = IoCManager.Resolve<GhostRoleManager>();
        if (args.Length != 3)
            return;

        var deleteEntities = bool.Parse(args[1]);
        var identifier = uint.Parse(args[2]);

        manager.DeleteGhostRoleGroup(player, identifier, deleteEntities);
    }

    private void ExecuteRelease(IConsoleShell shell,  IPlayerSession player, string argStr, string[] args)
    {
        var manager = IoCManager.Resolve<GhostRoleManager>();

        switch (args.Length)
        {
            case > 2:
                shell.WriteLine(Help);
                break;
            case 2:
            {
                var identifier = uint.Parse(args[1]);
                manager.ReleaseGhostRoleGroup(player, identifier);
                break;
            }
            default:
            {
                var identifier = manager.GetActiveGhostRoleGroupOrNull(player);
                if(identifier != null)
                    manager.ReleaseGhostRoleGroup(player, identifier.Value);
                break;
            }
        }
    }

    private void ExecuteOpen(IConsoleShell shell, IPlayerSession player, string argStr, string[] args)
    {
        EntitySystem.Get<GhostRoleGroupSystem>().OpenEui(player);
    }

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player == null)
        {
            shell.WriteLine("You can only manage ghost role groups on a client.");
            return;
        }

        if (args.Length < 1)
        {
            shell.WriteLine($"Usage: {Help}");
            return;
        }

        var player = (IPlayerSession) shell.Player;

        switch (args[0])
        {
            case "start":
                ExecuteStart(shell, player, argStr, args);
                break;
            case "release":
                ExecuteRelease(shell, player, argStr, args);
                break;
            case "delete":
                ExecuteDelete(shell, player, argStr, args);
                break;
            case "open":
                ExecuteOpen(shell, player, argStr, args);
                break;
            default:
                shell.WriteLine($"Usage: {Help}");
                break;
        }
    }
}
