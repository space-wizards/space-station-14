using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Silicons.Laws.Components;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Silicons.Laws;

[AdminCommand(AdminFlags.Logs)]
public sealed class ListLawsCommand : LocalizedCommands
{
        [Dependency] private readonly IEntityManager _entities = default!;
        [Dependency] private readonly IPlayerManager _players = default!;
    public override string Command => "lslaws";

    public override string Description => Loc.GetString("cmd-lslaws-description");

    public override string Help => Loc.GetString("cmd-lslaws-help");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-lslaws-invalid-arguments"));
            return;
        }

        if (!_players.TryGetSessionByUsername(args[0], out var player))
        {
            shell.WriteError(Loc.GetString("shell-target-player-does-not-exist"));
            return;
        }

        if (player.AttachedEntity == null ||
            !_entities.TryGetComponent<SiliconLawBoundComponent>(player.AttachedEntity.Value, out var comp))
        {
            shell.WriteError(Loc.GetString("shell-target-entity-does-not-have-message", ("missing", "SiliconLawBoundComponent")));
            return;
        }

        var borgSystem = _entities.System<SiliconLawSystem>();

        var laws = borgSystem.GetLaws(player.AttachedEntity.Value, comp);

        foreach (var law in laws.Laws)
        {
            shell.WriteLine($"Law {law.LawIdentifierOverride ?? law.Order.ToString()}: {Loc.GetString(law.LawString)}");

        }

    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), LocalizationManager.GetString("shell-argument-username-hint"));
        }

        return CompletionResult.Empty;
    }
}
