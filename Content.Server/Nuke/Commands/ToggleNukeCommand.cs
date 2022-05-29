using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.Console;

namespace Content.Server.Nuke.Commands
{
    [UsedImplicitly]
    [AdminCommand(AdminFlags.Fun)]
    public sealed class ToggleNukeCommand : IConsoleCommand
    {
        public string Command => "nukearm";
        public string Description => "Toggle nuclear bomb timer. You can set timer directly. Uid is optional.";
        public string Help => "nukearm <timer> <uid>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            EntityUid bombUid;
            NukeComponent? bomb = null;

            if (args.Length >= 2)
            {
                if (!EntityUid.TryParse(args[1], out bombUid))
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

                bombUid = bomb.Owner;
            }

            var nukeSys = EntitySystem.Get<NukeSystem>();
            if (args.Length >= 1)
            {
                if (!float.TryParse(args[0], out var timer))
                {
                    shell.WriteError("shell-argument-must-be-number");
                    return;
                }

                nukeSys.SetRemainingTime(bombUid, timer, bomb);
            }

            nukeSys.ToggleBomb(bombUid, bomb);
        }
    }
}
