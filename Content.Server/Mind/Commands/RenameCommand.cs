using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Mind.Commands;

[AdminCommand(AdminFlags.VarEdit)]
public sealed class RenameCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override string Command => "rename";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteLine(Help);
            return;
        }

        var name = args[1];
        var adminUid = shell.Player?.UserId.ToString() ?? "CONSOLE";
        var adminName = shell.Player?.Name ?? "CONSOLE";

        if (string.IsNullOrWhiteSpace(name))
        {
            shell.WriteLine("You cannot set an empty name.");
            _adminLogger.Add(LogType.Action, LogImpact.Low,
                $"Admin {adminName} (UID: {adminUid}) attempted to rename an entity to an empty name. Command rejected.");
            return;
        }

        if (name.Length > _cfgManager.GetCVar(CCVars.MaxNameLength))
        {
            shell.WriteLine(Loc.GetString("cmd-rename-too-long"));
            _adminLogger.Add(LogType.Action, LogImpact.Low,
                $"Admin {adminName} (UID: {adminUid}) attempted to rename an entity to a name that is too long. Command rejected.");
            return;
        }

        if (!TryParseUid(args[0], shell, _entManager, out var entityUid, adminName, adminUid))
            return;

        if (entityUid == null)
        {
            throw new InvalidOperationException("entityUid should never be null here.");
        }
        var uid = entityUid.Value;
        var oldName = _entManager.GetComponent<MetaDataComponent>(uid).EntityName ?? "unnamed";
        _metaSystem.SetEntityName(uid, name);
        var entityId = uid.ToString();
        var newName = string.IsNullOrEmpty(name) ? "unnamed" : name;

        _adminLogger.Add(LogType.Action, LogImpact.Medium,
            $"Admin {adminName} (UID: {adminUid}) renamed entity {entityId} from \"{oldName}\" to \"{newName}\"");
    }

    private bool TryParseUid(string str, IConsoleShell shell,
        IEntityManager entMan, out EntityUid? entityUid,
        string? adminName = null, string? adminUid = null)
    {
        if (NetEntity.TryParse(str, out var entityUidNet) && _entManager.TryGetEntity(entityUidNet, out entityUid) && entMan.EntityExists(entityUid))
            return true;

        if (_playerManager.TryGetSessionByUsername(str, out var session) && session.AttachedEntity.HasValue)
        {
            entityUid = session.AttachedEntity.Value;
            return true;
        }

        if (session == null)
            shell.WriteError(Loc.GetString("cmd-rename-not-found", ("target", str)));
        else
            shell.WriteError(Loc.GetString("cmd-rename-no-entity", ("target", str)));

        adminName ??= "UNKNOWN";
        adminUid ??= "UNKNOWN";
        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"Admin {adminName} (UID: {adminUid}) attempted to rename a non-existent entity or player (input: \"{str}\"). Command rejected.");

        entityUid = EntityUid.Invalid;
        return false;
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromOptions(CompletionHelper.SessionNames());

        return CompletionResult.Empty;
    }
}
