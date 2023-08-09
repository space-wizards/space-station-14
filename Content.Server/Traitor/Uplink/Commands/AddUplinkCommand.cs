using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.FixedPoint;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.Traitor.Uplink.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class AddUplinkCommand : IConsoleCommand
    {
        public string Command => "adduplink";

        public string Description => Loc.GetString("add-uplink-command-description");

        public string Help => Loc.GetString("add-uplink-command-help");


        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            return args.Length switch
            {
                1 => CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), Loc.GetString("add-uplink-command-completion-1")),
                2 => CompletionResult.FromHint(Loc.GetString("add-uplink-command-completion-2")),
                _ => CompletionResult.Empty
            };
        }

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length > 2)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            IPlayerSession? session;
            if (args.Length > 0)
            {
                // Get player entity
                if (!IoCManager.Resolve<IPlayerManager>().TryGetSessionByUsername(args[0], out session))
                {
                    shell.WriteLine(Loc.GetString("shell-target-player-does-not-exist"));
                    return;
                }
            }
            else
            {
                session = (IPlayerSession?) shell.Player;
            }

            if (session?.AttachedEntity is not { } user)
            {
                shell.WriteLine(Loc.GetString("add-uplink-command-error-1"));
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
            Logger.Debug(entityManager.ToPrettyString(user));
            // Finally add uplink
            var uplinkSys = entityManager.EntitySysManager.GetEntitySystem<UplinkSystem>();
            if (!uplinkSys.AddUplink(user, FixedPoint2.New(tcCount), uplinkEntity: uplinkEntity))
            {
                shell.WriteLine(Loc.GetString("add-uplink-command-error-2"));
            }
        }
    }
}
