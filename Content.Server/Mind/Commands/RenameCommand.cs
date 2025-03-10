using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration;
using Content.Shared.Access.Components;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.Mind.Commands;

[AdminCommand(AdminFlags.VarEdit)]
public sealed class RenameCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;

    public override string Command => "rename";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteLine(Help);
            return;
        }

        var name = args[1];
        if (name.Length > _cfgManager.GetCVar(CCVars.MaxNameLength))
        {
            shell.WriteLine(Loc.GetString("cmd-rename-too-long"));
            return;
        }

        if (!TryParseUid(args[0], shell, _entManager, out var entityUid))
            return;

        _metaSystem.SetEntityName(entityUid.Value, name);
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
}
