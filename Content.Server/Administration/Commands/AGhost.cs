using Content.Server.GameObjects.Components.Observer;
using Content.Server.Players;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public class AGhost : IClientCommand
    {
        public string Command => "aghost";
        public string Description => "Makes you an admin ghost.";
        public string Help => "aghost";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (player == null)
            {
                shell.SendText((IPlayerSession) null, "Nah");
                return;
            }

            var mind = player.ContentData().Mind;
            if (mind == null)
            {
                shell.SendText(player, "You can't ghost here!");
                return;
            }

            if (mind.VisitingEntity != null && mind.VisitingEntity.Prototype.ID == "AdminObserver")
            {
                var visiting = mind.VisitingEntity;
                mind.UnVisit();
                visiting.Delete();
            }
            else
            {
                var canReturn = mind.CurrentEntity != null && !mind.CurrentEntity.HasComponent<GhostComponent>();
                var entityManager = IoCManager.Resolve<IEntityManager>();
                var ghost = entityManager.SpawnEntity("AdminObserver", player.AttachedEntity.Transform.MapPosition);
                if(canReturn)
                    mind.Visit(ghost);
                else
                    mind.TransferTo(ghost);
                ghost.GetComponent<GhostComponent>().CanReturnToBody = canReturn;
            }
        }
    }
}
