using JetBrains.Annotations;
using Robust.Shared.Console;
using Content.Shared.Administration;
using Robust.Shared.Prototypes;
using Content.Shared._Starlight.CloudEmotes;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Player;

namespace Content.Server._Starlight.Commands;

[UsedImplicitly, AnyCommand]
public sealed class CloudEmoteCommand : LocalizedCommands
{
    [Dependency] private readonly IEntitySystemManager _entitySystems = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityNetworkManager _net = default!;
    public override string Command => "cloudemote";

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.PrototypeIDs<CloudEmotePrototype>(),
                Loc.GetString("cmd-cloudemote-hint-1"));
        }

        return CompletionResult.Empty;
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(LocalizationManager.GetString("shell-wrong-arguments-number"));
            return;
        }

        var player = shell.Player;
        if (player?.AttachedEntity == null)
        {
            shell.WriteLine(LocalizationManager.GetString("shell-only-players-can-run-this-command"));
            return;
        }

        var emote = args[0];
        if (!_prototypeManager.TryIndex<CloudEmotePrototype>(emote, out var _))
        {
            shell.WriteLine(LocalizationManager.GetString("cmd-cloudemote-invalid-emote"));
            return;
        }

        if (_entityManager.TryGetComponent<MobStateComponent>(player.AttachedEntity.Value, out var mobState))
            if (mobState.CurrentState != MobState.Alive)
                return;

        var msg = new CloudEmotesMessage(_entityManager.GetNetEntity(player.AttachedEntity.Value), emote);
        var filter = Filter.Pvs(player.AttachedEntity.Value);
        foreach (var session in filter.Recipients)
            _net.SendSystemNetworkMessage(msg, session.Channel);
    }
}