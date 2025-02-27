using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration;
using Content.Server.Mind;
using Content.Shared.Mind.Components;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
sealed class SwapMindCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;

    public override string Command => "swapmind";

    public override string Description => Loc.GetString("cmd-swapmind-command-description", ("requiredComponent", nameof(MindContainerComponent)));

    public override string Help => Loc.GetString("cmd-swapmind-command-help-text", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        // First player
        if (!TryParseUid(args[0], shell, _entManager, out var firstEntityUid))
            return;

        // Second player
        if (!TryParseUid(args[1], shell, _entManager, out var secondEntityUid))
            return;

        if (!_entManager.HasComponent<MindContainerComponent>(firstEntityUid) ||
            !_entManager.HasComponent<MindContainerComponent>(secondEntityUid))
        {
            shell.WriteLine(Loc.GetString("cmd-swapmind-command-target-has-no-mind-message"));
            return;
        }

        if (!_mindSystem.TryGetMind(firstEntityUid.Value, out var firstMindId, out var firstMindComponent))
        {
            shell.WriteLine(Loc.GetString("cmd-swapmind-command-minds-not-found"));
            return;
        }

        if (!_mindSystem.TryGetMind(secondEntityUid.Value, out var secondMindId, out var secondMindComponent))
        {
            shell.WriteLine(Loc.GetString("cmd-swapmind-command-minds-not-found"));
            return;
        }

        // Swap the minds
        _mindSystem.TransferTo(firstMindId, secondEntityUid);
        _mindSystem.TransferTo(secondMindId, firstEntityUid);
        shell.WriteLine(Loc.GetString("cmd-swapmind-success-message"));
    }

    private bool TryParseUid(string str, IConsoleShell shell,
        IEntityManager entMan, [NotNullWhen(true)] out EntityUid? entityUid)
    {
        entityUid = null;

        if (NetEntity.TryParse(str, out var entityUidNet) &&
            _entManager.TryGetEntity(entityUidNet, out entityUid) &&
            entMan.EntityExists(entityUid))
        {
            return true;
        }

        if (!_playerManager.TryGetSessionByUsername(str, out var session))
        {
            shell.WriteError(Loc.GetString("cmd-rename-not-found", ("target", str)));
            return false;
        }

        if (session.AttachedEntity == null)
        {
            shell.WriteError(Loc.GetString("cmd-rename-no-entity", ("target", str)));
            return false;
        }

        entityUid = session.AttachedEntity.Value;
        return true;
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), Loc.GetString("cmd-swapmind-command-hint"));
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), Loc.GetString("cmd-swapmind-command-hint"));
        }

        return CompletionResult.Empty;
    }
}
