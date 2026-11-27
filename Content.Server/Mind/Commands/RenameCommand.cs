using Content.Server.Administration.Logs;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Mind.Commands;

[AdminCommand(AdminFlags.VarEdit)]
public sealed partial class RenameCommand : LocalizedEntityCommands
{
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private IConfigurationManager _cfgManager = default!;
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private MetaDataSystem _metaSystem = default!;

    public override string Command => "rename";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteLine(Help);
            return;
        }

        var newName = args[1];
        if (newName.Length > _cfgManager.GetCVar(CCVars.MaxNameLength))
        {
            shell.WriteLine(Loc.GetString("cmd-rename-too-long"));
            return;
        }

        if (!TryParseUid(args[0], shell, _entManager, out var entityUid))
            return;

        var uid = entityUid.Value;
        var admin = shell.Player;

        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"Admin {admin} ({admin?.UserId}) renamed {uid} to \"{newName}\"");
        _metaSystem.SetEntityName(uid, newName);
    }

    private bool TryParseUid(string str, IConsoleShell shell,
        IEntityManager entMan, [NotNullWhen(true)] out EntityUid? entityUid)
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
