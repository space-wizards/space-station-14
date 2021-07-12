using Content.Server.Explosion;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Map;

#nullable enable

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public sealed class ExplosionCommand : IConsoleCommand
    {
        public string Command => "explode";
        public string Description => "Train go boom";
        public string Help => "Usage: explode <x> <y> <dev> <heavy> <light> <flash>\n" +
                              "The explosion happens on the same map as the user.";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player?.AttachedEntity == null)
            {
                shell.WriteLine("You must have an attached entity.");
                return;
            }

            var x = float.Parse(args[0]);
            var y = float.Parse(args[1]);

            var dev = int.Parse(args[2]);
            var hvy = int.Parse(args[3]);
            var lgh = int.Parse(args[4]);
            var fla = int.Parse(args[5]);

            var mapTransform = player.AttachedEntity.Transform.GetMapTransform();
            var coords = new EntityCoordinates(mapTransform.Owner.Uid, x, y);

            ExplosionHelper.SpawnExplosion(coords, dev, hvy, lgh, fla);
        }
    }
}
