#nullable enable
using Content.Server.Mind.Components;
using Content.Server.Players;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    class SetMindCommand : IConsoleCommand
    {
        public string Command => "setmind";

        public string Description => Loc.GetString("Transfers a mind to the specified entity. The entity must have a MindComponent.");

        public string Help => Loc.GetString("Usage: {0} <entityUid> <username>", Command);

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteLine(Loc.GetString("Wrong number of arguments."));
                return;
            }

            if (!int.TryParse(args[0], out var entityUid))
            {
                shell.WriteLine(Loc.GetString("EntityUid must be a number."));
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();

            var eUid = new EntityUid(entityUid);

            if (!eUid.IsValid() || !entityManager.EntityExists(eUid))
            {
                shell.WriteLine(Loc.GetString("Invalid entity ID."));
                return;
            }

            var target = entityManager.GetEntity(eUid);

            if (!target.TryGetComponent<MindComponent>(out var mindComponent))
            {
                shell.WriteLine(Loc.GetString("Target entity does not have a mind (did you forget to make sentient?)"));
                return;
            }

            if (!IoCManager.Resolve<IPlayerManager>().TryGetSessionByUsername(args[1], out var session))
            {
                shell.WriteLine(Loc.GetString("Target player does not exist"));
                return;
            }

            // hm, does player have a mind? if not we may need to give them one
            var playerCData = session.ContentData();
            if (playerCData == null)
            {
                shell.WriteLine(Loc.GetString("Target player does not have content data (wtf?)"));
                return;
            }

            var mind = playerCData.Mind;
            if (mind == null)
            {
                mind = new Mind.Mind(session.UserId)
                {
                    CharacterName = target.Name
                };
                playerCData.Mind = mind;
            }
            mind.TransferTo(target);
        }
    }
}
