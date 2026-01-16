using System.Linq;
using Content.Server.Administration;
using Content.Server.Body.Systems;
using Content.Shared.Administration;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Robust.Shared.Console;
using Robust.Shared.Random;

namespace Content.Server.Body.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public sealed class RemoveHandCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

        public override string Command => "removehand";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player == null)
            {
                shell.WriteLine(Loc.GetString("shell-only-players-can-run-this-command"));
                return;
            }

            if (player.AttachedEntity == null)
            {
                shell.WriteLine(Loc.GetString("shell-must-be-attached-to-entity"));
                return;
            }

            if (!EntityManager.TryGetComponent(player.AttachedEntity, out BodyComponent? body))
            {
                var text = Loc.GetString(
                    "cmd-removehand-no-body",
                    ("random", _random.Prob(0.2f) ? Loc.GetString("cmd-removehand-no-body-must-scream") : "."));

                shell.WriteLine(text);
                return;
            }

            var hand = _bodySystem.GetBodyChildrenOfType(player.AttachedEntity.Value, BodyPartType.Hand, body).FirstOrDefault();

            if (hand == default)
            {
                shell.WriteLine(Loc.GetString("cmd-removehand-no-hands"));
            }
            else
            {
                _transformSystem.AttachToGridOrMap(hand.Id);
            }
        }
    }
}
