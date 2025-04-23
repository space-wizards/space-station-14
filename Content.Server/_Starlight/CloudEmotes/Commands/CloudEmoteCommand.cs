using System.Linq;
using Content.Shared._Starlight.CloudEmote;
using Content.Shared.Administration;
using Content.Shared.Ghost;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server._Starlight.CloudEmotes.Commands
{
    [UsedImplicitly]
    [AnyCommand]
    public sealed class CloudEmoteCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntitySystemManager _entitySystems = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        public override string Command => "emote";
        public string[] emotes = { "lenny", "mark", "nervous" };

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            return args.Length switch
            {
                1 => CompletionResult.FromHintOptions(emotes,
                    LocalizationManager.GetString("cmd-emote-hint-1")),
                _ => CompletionResult.Empty,
            };
        }

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var transformSystem = _entitySystems.GetEntitySystem<TransformSystem>();

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
            
            if (!_entityManager.TryGetComponent<MobStateComponent>(player.AttachedEntity, out var state) 
                || state.CurrentState == MobState.Critical 
                || state.CurrentState == MobState.Dead)
            {
                shell.WriteLine(LocalizationManager.GetString("shell-only-alive-can-run-this-command"));
                return;
            }

            if (_entityManager.HasComponent<GhostComponent>(player.AttachedEntity))
            {
                shell.WriteLine(LocalizationManager.GetString("cmd-emote-ghost"));
                return;
            }

            var emote = args[0];
            if (!emotes.Contains(emote))
            {
                shell.WriteLine(LocalizationManager.GetString("cmd-emote-invalid-emote"));
                return;
            }

            if (_entityManager.HasComponent<CloudEmoteActiveComponent>(player.AttachedEntity.Value))
            {
                shell.WriteLine(LocalizationManager.GetString("cmd-emote-timeout"));
                return;
            }
            var emote_name = args[0] = "CloudEmote" + char.ToUpper(args[0][0]) + args[0].Substring(1);
            var comp = _entityManager.AddComponent<CloudEmoteActiveComponent>(player.AttachedEntity.Value);
            comp.EmoteName = emote_name;
        }
    }
}
