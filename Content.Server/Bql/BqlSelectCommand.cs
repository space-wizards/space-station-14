using System.Linq;
using Content.Server.Administration;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Bql;
using Content.Shared.Eui;
using Robust.Server.Bql;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Bql;

[AdminCommand(AdminFlags.Query)]
public sealed class BqlSelectCommand : LocalizedCommands
{
    [Dependency] private readonly IBqlQueryManager _bql = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override string Command => "bql_select";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player == null)
        {
            shell.WriteError(LocalizationManager.GetString("cmd-bql_select-err-server-shell"));
            return;
        }

        var (entities, rest) = _bql.SimpleParseAndExecute(argStr["bql_select".Length..]);

        if (!string.IsNullOrWhiteSpace(rest))
            shell.WriteLine(LocalizationManager.GetString("cmd-bql_select-err-rest", ("rest", rest)));

        var ui = new BqlResultsEui(
            entities.Select(e => (_entityManager.GetComponent<MetaDataComponent>(e).EntityName, e)).ToArray()
        );
        _euiManager.OpenEui(ui, (IPlayerSession) shell.Player);
        _euiManager.QueueStateUpdate(ui);
    }
}

internal sealed class BqlResultsEui : BaseEui
{
    private readonly (string name, EntityUid entity)[] _entities;

    public BqlResultsEui((string name, EntityUid entity)[] entities)
    {
        _entities = entities;
    }

    public override EuiStateBase GetNewState()
    {
        return new BqlResultsEuiState(_entities);
    }
}
