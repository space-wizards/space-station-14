using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Emoting;
using Content.Shared.Examine;
using Content.Shared.Mind.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Speech;
using Robust.Shared.Console;

namespace Content.Server.Mind.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class MakeSentientCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        public string Command => "makesentient";
        public string Description => "Makes an entity sentient (able to be controlled by a player)";
        public string Help => "makesentient <entity id> [allowMovement = false] [allowSpeech = true]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1 || args.Length > 3)
            {
                shell.WriteLine($"Wrong number of arguments.\n{Help}");
                return;
            }

            if (!NetEntity.TryParse(args[0], out var entNet) || !_entManager.TryGetEntity(entNet, out var entId))
            {
                shell.WriteLine("Invalid argument.");
                return;
            }

            if (!_entManager.EntityExists(entId))
            {
                shell.WriteLine("Invalid entity specified!");
                return;
            }

            var allowMovement = false;
            var allowSpeech = true;
            if (args.Length >= 2 && !bool.TryParse(args[1], out allowMovement))
            {
                shell.WriteLine($"Optional argument 2 \"allowMovement\" must be \"true\" or \"false\".\n{Help}");
                return;
            }
            if (args.Length >= 3 && !bool.TryParse(args[2], out allowSpeech))
            {
                shell.WriteLine($"Optional argument 3 \"allowSpeech\" must be \"true\" or \"false\".\n{Help}");
                return;
            }

            MakeSentient(entId.Value, _entManager, allowMovement, allowSpeech);
        }

        public static void MakeSentient(EntityUid uid, IEntityManager entityManager, bool allowMovement = true, bool allowSpeech = true)
        {
            entityManager.EnsureComponent<MindContainerComponent>(uid);
            if (allowMovement)
            {
                entityManager.EnsureComponent<InputMoverComponent>(uid);
                entityManager.EnsureComponent<MobMoverComponent>(uid);
                entityManager.EnsureComponent<MovementSpeedModifierComponent>(uid);
            }

            if (allowSpeech)
            {
                entityManager.EnsureComponent<SpeechComponent>(uid);
                entityManager.EnsureComponent<EmotingComponent>(uid);
            }

            entityManager.EnsureComponent<ExaminerComponent>(uid);
        }
    }
}
