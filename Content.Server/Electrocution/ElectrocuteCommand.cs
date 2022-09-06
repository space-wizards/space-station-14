using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.StatusEffect;
using Robust.Shared.Console;

namespace Content.Server.Electrocution
{
    [AdminCommand(AdminFlags.Fun)]
    public sealed class ElectrocuteCommand : IConsoleCommand
    {
        public string Command => "electrocute";
        public string Description => Loc.GetString("electrocute-command-description");
        public string Help => $"{Command} <uid> <seconds> <damage>";

        public const string ElectrocutionStatusEffect = "Electrocution";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                // TODO: Localize this.
                shell.WriteError("Not enough arguments!");
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!EntityUid.TryParse(args[0], out var uid) || !entityManager.EntityExists(uid))
            {
                shell.WriteError($"Invalid entity specified!");
                return;
            }

            if (!entityManager.EntitySysManager.GetEntitySystem<StatusEffectsSystem>().CanApplyEffect(uid, ElectrocutionStatusEffect))
            {
                shell.WriteError(Loc.GetString("electrocute-command-entity-cannot-be-electrocuted"));
                return;
            }

            if (args.Length < 2 || !int.TryParse(args[1], out var seconds))
            {
                seconds = 10;
            }

            if (args.Length < 3 || !int.TryParse(args[2], out var damage))
            {
                damage = 10;
            }

            entityManager.EntitySysManager.GetEntitySystem<ElectrocutionSystem>()
                .TryDoElectrocution(uid, null, damage, TimeSpan.FromSeconds(seconds), refresh: true, ignoreInsulation: true);
        }
    }
}
