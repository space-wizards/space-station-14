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

        public string Description => Loc.GetString("set-mind-command-description", ("requiredComponent", nameof(MindComponent)));

        public string Help => Loc.GetString("set-mind-command-help-text", ("command", Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            if (!int.TryParse(args[0], out var entityUid))
            {
                shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();

            var eUid = new EntityUid(entityUid);

            if (!eUid.IsValid() || !entityManager.EntityExists(eUid))
            {
                shell.WriteLine(Loc.GetString("shell-invalid-entity-id"));
                return;
            }

            var target = entityManager.GetEntity(eUid);

            if (!target.HasComponent<MindComponent>())
            {
                shell.WriteLine(Loc.GetString("set-mind-command-target-has-no-mind-message"));
                return;
            }

            if (!IoCManager.Resolve<IPlayerManager>().TryGetSessionByUsername(args[1], out var session))
            {
                shell.WriteLine(Loc.GetString("shell-target-player-does-not-exist"));
                return;
            }

            // hm, does player have a mind? if not we may need to give them one
            var playerCData = session.ContentData();
            if (playerCData == null)
            {
                shell.WriteLine(Loc.GetString("set-mind-command-target-has-no-content-data-message"));
                return;
            }

            var mind = playerCData.Mind;
            if (mind == null)
            {
                mind = new Mind.Mind(session.UserId)
                {
                    CharacterName = target.Name
                };
                mind.ChangeOwningPlayer(session.UserId);
            }
            mind.TransferTo(target.Uid);
        }
    }
}
