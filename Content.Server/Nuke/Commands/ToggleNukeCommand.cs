using Content.Server.Administration;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using System.Linq;

namespace Content.Server.Nuke.Commands
{
    [UsedImplicitly]
    [AdminCommand(AdminFlags.Fun)]
    public class ToggleNukeCommand : IConsoleCommand
    {
        public string Command => "armnuke";
        public string Description => "Toggle nuclear bomb timer";
        public string Help => "armnuke <uid>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            EntityUid bombUid;
            NukeComponent? bomb = null;

            if (args.Length > 1)
            {
                if (!EntityUid.TryParse(args[0], out bombUid))
                {
                    shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
                    return;
                }
            }
            else
            {
                var entManager = IoCManager.Resolve<IEntityManager>();
                var bombs = entManager.EntityQuery<NukeComponent>();

                bomb = bombs.FirstOrDefault();
                if (bomb == null)
                {
                    shell.WriteError("Can't find any entity with a NukeComponent");
                    return;
                }

                bombUid = bomb.OwnerUid;
            }

            EntitySystem.Get<NukeSystem>().ToggleBomb(bombUid, bomb);
        }
    }
}
