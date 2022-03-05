using Content.Server.Administration;
using Content.Server.Traitor.Uplink.Account;
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
    public sealed class AddUplinkCommand : IConsoleCommand
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
            if (session.AttachedEntity is not {} user)
            {
                shell.WriteLine(Loc.GetString("Selected player doesn't controll any entity"));
                return;
            }

            // Get target item
            EntityUid? uplinkEntity = null;
            var entityManager = IoCManager.Resolve<IEntityManager>();
            if (args.Length >= 2)
            {
                if (!int.TryParse(args[1], out var itemID))
                {
                    shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
                    return;
                }

                var eUid = new EntityUid(itemID);
                if (!eUid.IsValid() || !entityManager.EntityExists(eUid))
                {
                    shell.WriteLine(Loc.GetString("shell-invalid-entity-id"));
                    return;
                }

                uplinkEntity = eUid;
            }

            // Get TC count
            var configManager = IoCManager.Resolve<IConfigurationManager>();
            var tcCount = configManager.GetCVar(CCVars.TraitorStartingBalance);

            // Get account
            var uplinkAccount = new UplinkAccount(tcCount, user);
            var accounts = entityManager.EntitySysManager.GetEntitySystem<UplinkAccountsSystem>();
            accounts.AddNewAccount(uplinkAccount);

            // Finally add uplink
            if (!entityManager.EntitySysManager.GetEntitySystem<UplinkSystem>()
                .AddUplink(user, uplinkAccount, uplinkEntity))
            {
                shell.WriteLine(Loc.GetString("Failed to add uplink to the player"));
                return;
            }
        }
    }
}
