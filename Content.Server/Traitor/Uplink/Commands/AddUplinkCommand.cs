using Content.Server.Administration;
using Content.Server.PDA.Managers;
using Content.Server.Traitor.Uplink.Components;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Traitor.Uplink;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Server.Traitor.Uplink.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public class AddUplinkCommand : IConsoleCommand
    {
        public string Command => "adduplink";

        public string Description => "Creates uplink on selected item and link it to users account";

        public string Help => "Usage: adduplink <username> <item-id>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            // Get player entity
            if (!IoCManager.Resolve<IPlayerManager>().TryGetSessionByUsername(args[0], out var session))
            {
                shell.WriteLine(Loc.GetString("shell-target-player-does-not-exist"));
                return;
            }
            if (session.AttachedEntity == null)
            {
                shell.WriteLine(Loc.GetString("Selected player doesn't controll any entity"));
                return;
            }
            var player = session.AttachedEntity;

            // Get target item
            IEntity uplinkEntity;
            if (args.Length >= 2)
            {
                if (!int.TryParse(args[1], out var itemID))
                {
                    shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
                    return;
                }

                var entityManager = IoCManager.Resolve<IEntityManager>();
                var eUid = new EntityUid(itemID);
                if (!eUid.IsValid() || !entityManager.EntityExists(eUid))
                {
                    shell.WriteLine(Loc.GetString("shell-invalid-entity-id"));
                    return;
                }

                uplinkEntity = entityManager.GetEntity(eUid);
            }
            else
            {
                // TODO: check players inventory and find PDA
                return;
            }

            // Get TC count
            var configManager = IoCManager.Resolve<IConfigurationManager>();
            var tcCount = configManager.GetCVar(CCVars.TraitorStartingBalance);

            // Try to create new account
            // TODO: check if account already exist
            var uplinkManager = IoCManager.Resolve<IUplinkManager>();
            var uplinkAccount = new UplinkAccount(player.Uid, tcCount);
            uplinkManager.AddNewAccount(uplinkAccount);

            var uplink = uplinkEntity.AddComponent<UplinkComponent>();
            uplink.UplinkAccount = uplinkAccount;
        }
    }
}
