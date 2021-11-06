using Content.Server.Mind.Components;
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
        public string Description => Loc.GetString("control-mob-command-description");
        public string Help => Loc.GetString("control-mob-command-help-text");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine("shell-server-cannot");
                return;
            }

            if (args.Length != 1)
            {
                shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }


            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!int.TryParse(args[0], out var targetId))
            {
                shell.WriteLine(Loc.GetString("shell-argument-must-be-number"));
                return;
            }

            var eUid = new EntityUid(targetId);

            if (!eUid.IsValid() || !entityManager.EntityExists(eUid))
            {
                shell.WriteLine(Loc.GetString("shell-invalid-entity-id"));
                return;
            }

            var target = entityManager.GetEntity(eUid);
            if (!target.TryGetComponent(out MindComponent? mindComponent))
            {
                shell.WriteLine(Loc.GetString("shell-entity-is-not-mob"));
                return;
            }

            var mind = player.ContentData()?.Mind;

            DebugTools.AssertNotNull(mind);

            mind!.TransferTo(target);
        }
    }
}
