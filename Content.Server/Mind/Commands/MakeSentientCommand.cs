using Content.Server.Administration;
using Content.Server.AI.Components;
using Content.Server.Mind.Components;
using Content.Shared.Administration;
using Content.Shared.Emoting;
using Content.Shared.Examine;
using Content.Shared.Movement.Components;
using Content.Shared.Speech;
using Robust.Shared.Console;

namespace Content.Server.Mind.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public sealed class MakeSentientCommand : IConsoleCommand
    {
        public string Command => "makesentient";
        public string Description => "Makes an entity sentient (able to be controlled by a player)";
        public string Help => "makesentient <entity id>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine("Wrong number of arguments.");
                return;
            }

            if (!EntityUid.TryParse(args[0], out var entId))
            {
                shell.WriteLine("Invalid argument.");
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!entityManager.EntityExists(entId))
            {
                shell.WriteLine("Invalid entity specified!");
                return;
            }

            MakeSentient(entId, entityManager);
        }

        public static void MakeSentient(EntityUid uid, IEntityManager entityManager)
        {
            entityManager.RemoveComponent<NPCComponent>(uid);

            entityManager.EnsureComponent<MindComponent>(uid);
            entityManager.EnsureComponent<InputMoverComponent>(uid);
            entityManager.EnsureComponent<MobMoverComponent>(uid);
            entityManager.EnsureComponent<MovementSpeedModifierComponent>(uid);
            entityManager.EnsureComponent<SharedSpeechComponent>(uid);
            entityManager.EnsureComponent<SharedEmotingComponent>(uid);
            entityManager.EnsureComponent<ExaminerComponent>(uid);
        }
    }
}
