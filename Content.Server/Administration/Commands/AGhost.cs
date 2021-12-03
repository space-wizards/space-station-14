using Content.Server.GameTicking;
using Content.Server.Ghost.Components;
using Content.Server.Players;
using Content.Shared.Administration;
using Content.Shared.Ghost;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

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

            if (mind.VisitingEntity != null && IoCManager.Resolve<IEntityManager>().HasComponent<GhostComponent>(mind.VisitingEntity.Uid))
            {
                player.ContentData()!.Mind?.UnVisit();
                return;
            }

            var canReturn = mind.CurrentEntity != null;
            IEntity? tempQualifier = player.AttachedEntity;
            var ghost = IoCManager.Resolve<IEntityManager>().SpawnEntity((string?) "AdminObserver", (EntityCoordinates) ((tempQualifier != null ? IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(tempQualifier.Uid) : null).Coordinates
                ?? EntitySystem.Get<GameTicker>().GetObserverSpawnPoint()));

            if (canReturn)
            {
                // TODO: Remove duplication between all this and "GamePreset.OnGhostAttempt()"...
                if(!string.IsNullOrWhiteSpace(mind.CharacterName))
                    IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(ghost.Uid).EntityName = mind.CharacterName;
                else if (!string.IsNullOrWhiteSpace(mind.Session?.Name))
                    IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(ghost.Uid).EntityName = mind.Session.Name;

                mind.Visit(ghost);
            }
            else
            {
                IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(ghost.Uid).EntityName = player.Name;
                mind.TransferTo(ghost.Uid);
            }

            var comp = IoCManager.Resolve<IEntityManager>().GetComponent<GhostComponent>(ghost.Uid);
            EntitySystem.Get<SharedGhostSystem>().SetCanReturnToBody(comp, canReturn);
        }
    }
}
