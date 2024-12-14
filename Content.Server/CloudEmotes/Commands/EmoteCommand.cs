using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.Console;
using Content.Shared.Administration;
using Robust.Shared.Map;

namespace Content.Server.CloudEmotes.Commands
{
    [UsedImplicitly]
    [AnyCommand]
    public sealed class EmoteCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntitySystemManager _entitySystems = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly SharedTransformSystem _transform = _entitySystems.GetEntitySystem<SharedTransformSystem>()!;
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
            if (!emotes.Contains(emote))
            {
                shell.WriteLine(LocalizationManager.GetString("cmd-emote-invalid-emote"));
                return;
            }
            // TODO THIS SHOULD ADD COMPONENT, AND THEN CLIENT SYSTEM WILL DRAW IT BASED ON COMPONENTS (AND WILL KEEP IT UPDATED ON NEW LOCATION DUE TO IT BEING IN COMP SAVED LIKE TETHER MOVING)
            var emote_name = args[0] = "CloudEmote" + char.ToUpper(args[0][0]) + args[0].Substring(1);
            var pos = _transform.GetMapCoordinates(player.AttachedEntity.Value);
            var emote_entity = _entityManager.Spawn(emote_name, pos);
            _transform.SetCoordinates(emote_entity, new EntityCoordinates(player.AttachedEntity.Value, new System.Numerics.Vector2(0f, 1f)));
        }
    }
}
