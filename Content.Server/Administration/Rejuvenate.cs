using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.GameObjects;
using Content.Server.Players;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Administration
{
    class Rejuvenate : IClientCommand
    {
        public string Command => "rejuvenate";
        public string Description => "Fully heals a mob.";
        public string Help => "rejuvenate <mobUid_1> <mobUid_2> ... <mobUid_n>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (player == null)
            {
                shell.SendText((IPlayerSession)null, "Player session is null.");
                return;
            }
            if (args.Length < 1)
            {
                shell.SendText(player, Description);
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            if (entityManager == null)
            {
                shell.SendText(player, "Couldn't resolve IEntityManager.");
                return;
            }

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                var entity = entityManager.GetEntity(EntityUid.Parse(arg));
                if (!entity.TryGetComponent(out DamageableComponent damage))
                {
                    shell.SendText(player, $"Entity at argument {i} does not have a DamageableComponent.");
                    continue;
                }
                damage.HealAllDamage();
            }
        }
    }
}
