using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Observer;
using Content.Server.Players;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    class ControlMob : IClientCommand
    {
        public string Command => "controlmob";
        public string Description => Loc.GetString("Transfers user mind to the specified entity.");
        public string Help => Loc.GetString("Usage: controlmob <mobUid>.");

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (player == null)
            {
                shell.SendText((IPlayerSession) null, "Server cannot do this.");
                return;
            }

            if (args.Length != 1)
            {
                shell.SendText(player, Loc.GetString("Wrong number of arguments."));
                return;
            }


            var mind = player.ContentData().Mind;
            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!int.TryParse(args[0], out var targetId))
            {
                shell.SendText(player, Loc.GetString("Argument must be a number."));
                return;
            }

            var eUid = new EntityUid(targetId);

            if (!eUid.IsValid() || !entityManager.EntityExists(eUid))
            {
                shell.SendText(player, Loc.GetString("Invalid entity ID."));
                return;
            }

            var target = entityManager.GetEntity(eUid);
            if (!target.TryGetComponent(out MindComponent mindComponent))
            {
                shell.SendText(player, Loc.GetString("Target entity is not a mob!"));
                return;
            }

            var oldEntity = mind.CurrentEntity;

            mindComponent.Mind?.TransferTo(null);
            mind.TransferTo(target);

            if(oldEntity.HasComponent<GhostComponent>())
                oldEntity.Delete();

        }
    }
}
