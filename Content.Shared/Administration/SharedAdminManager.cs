using System.Linq;
using System.Reflection;
using Content.Shared.Administration.Managers;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Player;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Content.Shared.Administration;

public abstract class SharedAdminManager : ISharedAdminManager, IConGroupControllerImplementation
{
    [Dependency] protected readonly IConsoleHost ConsoleHost = default!;
    [Dependency] protected readonly ToolshedManager Toolshed = default!;
    [Dependency] protected readonly ILocalizationManager Loc = default!;
    [Dependency] protected readonly IResourceManager ResMan = default!;
    [Dependency] protected readonly ISharedPlayerManager PlayerMan = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IConGroupController _conGroup = default!;

    protected readonly AdminCommandPermissions CommandPermissions = new();
    protected readonly AdminCommandPermissions ToolshedCommandPermissions = new();
    protected ISawmill Log = default!;

    public bool Initialized { get; private set; }

    public virtual void Initialize()
    {
        if (Initialized)
            throw new InvalidOperationException("Already initialized.");
        Initialized = true;
        _conGroup.Implementation = this;
        Toolshed.ActivePermissionController = this;

        Log = _logManager.GetSawmill("admin");
        ReloadCommandPermissions();
        ReloadToolshedPermissions();
        Toolshed.CommandsLoaded += ReloadToolshedPermissions;
    }

    public virtual void ReloadCommandPermissions()
    {
        CommandPermissions.Clear();

        foreach (var (cmdName, cmd) in ConsoleHost.RegisteredCommands)
        {
            var (isAvail, flagsReq) = GetRequiredFlags(cmd);
            if (!isAvail)
                continue;

            if (flagsReq.Length != 0)
                CommandPermissions.AdminCommands.Add(cmdName, flagsReq);
            else
                CommandPermissions.AnyCommands.Add(cmdName);
        }

        // Load flags for engine commands, since those don't have the attributes.
        if (ResMan.TryContentFileRead(new ResPath("/engineCommandPerms.yml"), out var efs))
            CommandPermissions.LoadPermissionsFromStream(efs);
    }

    public virtual void ReloadToolshedPermissions()
    {
        if (!Toolshed.Started)
            return;

        ToolshedCommandPermissions.Clear();
        foreach (var spec in Toolshed.DefaultEnvironment.AllCommands())
        {
            var (isAvail, flagsReq) = GetRequiredFlags(spec);
            if (!isAvail)
                continue;

            if (flagsReq.Length != 0)
                ToolshedCommandPermissions.AdminCommands.TryAdd(spec.Cmd.Name, flagsReq);
            else
                ToolshedCommandPermissions.AnyCommands.Add(spec.Cmd.Name);
        }

        if (ResMan.TryContentFileRead(new ResPath("/toolshedEngineCommandPerms.yml"), out var toolshedPerms))
            ToolshedCommandPermissions.LoadPermissionsFromStream(toolshedPerms);
    }

    public bool IsAdmin(ICommonSession session, bool includeDeAdmin = false)
    {
        return GetAdminData(session, includeDeAdmin) != null;
    }

    public abstract AdminData? GetAdminData(ICommonSession session, bool includeDeAdmin = false);

    public AdminData? GetAdminData(EntityUid uid, bool includeDeAdmin = false)
    {
        return PlayerMan.TryGetSessionByEntity(uid, out var session)
            ? GetAdminData(session, includeDeAdmin)
            : null;
    }

    public bool TryGetCommandFlags(CommandSpec command, out AdminFlags[]? flags)
    {
        var cmdName = command.Cmd.Name;

        if (ToolshedCommandPermissions.AnyCommands.Contains(cmdName))
        {
            // Anybody can use this command.
            flags = null;
            return true;
        }

        if (ToolshedCommandPermissions.AdminCommands.TryGetValue(cmdName, out flags))
        {
            return true;
        }

        flags = null;
        return false;
    }

    public virtual bool CanCommand(ICommonSession session, string cmdName)
    {
        return CommandPermissions.CanCommand(cmdName, GetAdminData(session));
    }

    public bool CheckInvokable(CommandSpec command, ICommonSession? user, out IConError? error)
    {
        if (user is null)
        {
            error = null;
            return true; // Server console.
        }

        if (!TryGetCommandFlags(command, out var flags))
        {
            // Command is missing permissions.
            error = new CommandPermissionsUnassignedError(command);
            return false;
        }

        if (flags is null)
        {
            // Anyone can execute this.
            error = null;
            return true;
        }

        var data = GetAdminData(user);
        if (data == null)
        {
            error = new NoPermissionError(Loc.GetString("cmd-insufficient-permissions", ("cmd", command.FullName())));
            return false;
        }

        foreach (var flag in flags)
        {
            if (data.HasFlag(flag))
            {
                error = null;
                return true;
            }
        }

        error = new NoPermissionError(Loc.GetString("cmd-insufficient-permissions", ("cmd", command.FullName())));
        return false;
    }

    private (bool isAvail, AdminFlags[] flagsReq) GetRequiredFlags(object cmd)
    {
        if (cmd is ConsoleHost.RegisteredCommand registered)
            return GetCallbackFlags(registered); // Method registered as command IConsoleHost.Register

        var type = cmd switch
        {
            ToolshedProxyCommand proxy => proxy.Spec.Cmd.GetType(), // Toolshed command
            CommandSpec spec => spec.Cmd.GetType(), // Toolshed command,
            _ => cmd.GetType(), // Normal IConsoleCommand or some other object
        };

        if (type.HasCustomAttribute<AnyCommandAttribute>())
        {
            // Available to everybody.
            return (true, []);
        }

        var attribs = type.GetCustomAttributes<AdminCommandAttribute>()
            .Select(p => p.Flags)
            .ToArray();

        // If attribs.length == 0 then no access attribute is specified, meaning that this is a server-only command.
        return (attribs.Length != 0, attribs);
    }

    private (bool isAvail, AdminFlags[] flagsReq) GetCallbackFlags(ConsoleHost.RegisteredCommand cmd)
    {
        if (cmd.HasCustomAttribute<AnyCommandAttribute>())
            return (true, []);

        var attribs = cmd.GetCustomAttributes<AdminCommandAttribute>()
            .Select(p => p.Flags)
            .ToArray();

        // If attribs.length == 0 then no access attribute is specified, meaning that this is a server-only command.
        return (attribs.Length != 0, attribs);
    }

    public bool CanViewVar(ICommonSession session)
    {
        return CanCommand(session, "vv");
    }

    public bool CanAdminPlace(ICommonSession session)
    {
        return GetAdminData(session)?.CanAdminPlace() ?? false;
    }

    public bool CanScript(ICommonSession session)
    {
        return GetAdminData(session)?.CanScript() ?? false;
    }

    public bool CanAdminMenu(ICommonSession session)
    {
        return GetAdminData(session)?.CanAdminMenu() ?? false;
    }

    public bool CanAdminReloadPrototypes(ICommonSession session)
    {
        return GetAdminData(session)?.CanAdminReloadPrototypes() ?? false;
    }
}

public sealed class CommandPermissionsUnassignedError(CommandSpec cmd) : ConError
{
    public override FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkupOrThrow(
            $"The command {cmd.FullName()} is missing permission flags and cannot be executed.");
    }
}

public sealed class NoPermissionError(string msg) : ConError
{
    public override FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkupOrThrow(msg);
    }
}
