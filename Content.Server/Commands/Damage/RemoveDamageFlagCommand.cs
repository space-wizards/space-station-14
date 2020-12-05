#nullable enable
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;

namespace Content.Server.Commands.Damage
{
    [AdminCommand(AdminFlags.Fun)]
    public class RemoveDamageFlagCommand : DamageFlagCommand
    {
        public override string Command => "removedamageflag";
        public override string Description => "Removes a damage flag from your entity or another.";
        public override string Help => $"Usage: {Command} <flag> / {Command} <entityUid> <flag>";

        public override void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (!TryGetEntity(shell, player, args, false, out var entity, out var flag, out var damageable))
            {
                return;
            }

            damageable.RemoveFlag(flag);
            shell.SendText(player, $"Removed damage flag {flag} from entity {entity.Name}");
        }
    }
}
