using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Players;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    class ControlMob : IConsoleCommand
    {
        public string Command => "controlmob";
        public string Description => Loc.GetString("Transfers user mind to the specified entity.");
        public string Help => Loc.GetString("Usage: controlmob <mobUid>.");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine("Server cannot do this.");
                return;
            }

            if (args.Length != 1)
            {
                shell.WriteLine(Loc.GetString("Wrong number of arguments."));
                return;
            }


            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!int.TryParse(args[0], out var targetId))
            {
                shell.WriteLine(Loc.GetString("Argument must be a number."));
                return;
            }

            var eUid = new EntityUid(targetId);

            if (!eUid.IsValid() || !entityManager.EntityExists(eUid))
            {
                shell.WriteLine(Loc.GetString("Invalid entity ID."));
                return;
            }

            var target = entityManager.GetEntity(eUid);
            if (!target.TryGetComponent(out MindComponent? mindComponent))
            {
                shell.WriteLine(Loc.GetString("Target entity is not a mob!"));
                return;
            }

            var mind = player.ContentData()?.Mind;

            DebugTools.AssertNotNull(mind);

            mind!.TransferTo(target);
        }
    }
}
