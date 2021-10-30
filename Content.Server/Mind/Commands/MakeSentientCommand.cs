using Content.Server.Administration;
using Content.Server.AI.Components;
using Content.Server.Mind.Components;
using Content.Shared.Administration;
using Content.Shared.Emoting;
using Content.Shared.Examine;
using Content.Shared.Movement.Components;
using Content.Shared.Speech;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Mind.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public class MakeSentientCommand : IConsoleCommand
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

            if (!int.TryParse(args[0], out var id))
            {
                shell.WriteLine("Invalid argument.");
                return;
            }

            var entId = new EntityUid(id);

            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!entityManager.TryGetEntity(entId, out var entity) || entity.Deleted)
            {
                shell.WriteLine("Invalid entity specified!");
                return;
            }

            MakeSentient(entity);
        }

        public static void MakeSentient(IEntity entity)
        {
            if(entity.HasComponent<AiControllerComponent>())
                entity.RemoveComponent<AiControllerComponent>();

            // Delay spawning these components to avoid race conditions with the deferred removal of AiController.
            Timer.Spawn(100, () =>
            {
                entity.EnsureComponent<MindComponent>();
                entity.EnsureComponent<SharedPlayerInputMoverComponent>();
                entity.EnsureComponent<SharedPlayerMobMoverComponent>();
                entity.EnsureComponent<SharedSpeechComponent>();
                entity.EnsureComponent<SharedEmotingComponent>();
                entity.EnsureComponent<ExaminerComponent>();
            });
        }
    }
}
