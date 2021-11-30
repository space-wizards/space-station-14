using Content.Server.GameTicking;
using Content.Server.Ghost.Components;
using Content.Server.Players;
using Content.Shared.Administration;
using Content.Shared.Ghost;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public class AGhost : IConsoleCommand
    {
        public string Command => "aghost";
        public string Description => "Makes you an admin ghost.";
        public string Help => "aghost";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine("Nah");
                return;
            }

            var mind = player.ContentData()?.Mind;

            if (mind == null)
            {
                shell.WriteLine("You can't ghost here!");
                return;
            }

            if (mind.VisitingEntity != null && mind.VisitingEntity.HasComponent<GhostComponent>())
            {
                player.ContentData()!.Mind?.UnVisit();
                return;
            }

            var canReturn = mind.CurrentEntity != null;
            var ghost = IoCManager.Resolve<IEntityManager>()
                .SpawnEntity("AdminObserver", player.AttachedEntity?.Transform.Coordinates
                                              ?? EntitySystem.Get<GameTicker>().GetObserverSpawnPoint());

            if (canReturn)
            {
                // TODO: Remove duplication between all this and "GamePreset.OnGhostAttempt()"...
                if(!string.IsNullOrWhiteSpace(mind.CharacterName))
                    ghost.Name = mind.CharacterName;
                else if (!string.IsNullOrWhiteSpace(mind.Session?.Name))
                    ghost.Name = mind.Session.Name;

                mind.Visit(ghost);
            }
            else
            {
                ghost.Name = player.Name;
                mind.TransferTo(ghost.Uid);
            }

            var comp = ghost.GetComponent<GhostComponent>();
            EntitySystem.Get<SharedGhostSystem>().SetCanReturnToBody(comp, canReturn);
        }
    }
}
