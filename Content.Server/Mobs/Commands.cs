using System.Text;
using Content.Server.Administration;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Mobs.Roles;
using Content.Server.Players;
using Content.Shared.Administration;
using Content.Shared.Roles;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.Mobs
{

    [AdminCommand(AdminFlags.Admin)]
    public class MindInfoCommand : IClientCommand
    {
        public string Command => "mindinfo";

        public string Description => "Lists info for the mind of a specific player.";

        public string Help => "mindinfo <session ID>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length != 1)
            {
                shell.SendText(player, "Expected exactly 1 argument.");
                return;
            }

            var mgr = IoCManager.Resolve<IPlayerManager>();
            if (mgr.TryGetSessionByUsername(args[0], out var data))
            {
                var mind = data.ContentData().Mind;

                var builder = new StringBuilder();
                builder.AppendFormat("player: {0}, mob: {1}\nroles: ", mind.UserId, mind.OwnedMob?.Owner?.Uid);
                foreach (var role in mind.AllRoles)
                {
                    builder.AppendFormat("{0} ", role.Name);
                }

                shell.SendText(player, builder.ToString());
            }
            else
            {
                shell.SendText(player, "Can't find that mind");
            }
        }
    }

    [AdminCommand(AdminFlags.Fun)]
    public class AddRoleCommand : IClientCommand
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public string Command => "addrole";

        public string Description => "Adds a role to a player's mind.";

        public string Help => "addrole <session ID> <Role Type>\nThat role type is the actual C# type name.";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length != 2)
            {
                shell.SendText(player, "Expected exactly 2 arguments.");
                return;
            }

            var mgr = IoCManager.Resolve<IPlayerManager>();
            if (mgr.TryGetPlayerDataByUsername(args[0], out var data))
            {
                var mind = data.ContentData().Mind;
                var role = new Job(mind, _prototypeManager.Index<JobPrototype>(args[1]));
                mind.AddRole(role);
            }
            else
            {
                shell.SendText(player, "Can't find that mind");
            }
        }
    }

    [AdminCommand(AdminFlags.Fun)]
    public class RemoveRoleCommand : IClientCommand
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public string Command => "rmrole";

        public string Description => "Removes a role from a player's mind.";

        public string Help => "rmrole <session ID> <Role Type>\nThat role type is the actual C# type name.";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length != 2)
            {
                shell.SendText(player, "Expected exactly 2 arguments.");
                return;
            }

            var mgr = IoCManager.Resolve<IPlayerManager>();
            if (mgr.TryGetPlayerDataByUsername(args[0], out var data))
            {
                var mind = data.ContentData().Mind;
                var role = new Job(mind, _prototypeManager.Index<JobPrototype>(args[1]));
                mind.RemoveRole(role);
            }
            else
            {
                shell.SendText(player, "Can't find that mind");
            }
        }
    }

    [AdminCommand(AdminFlags.Debug)]
    public class AddOverlayCommand : IClientCommand
    {
        public string Command => "addoverlay";
        public string Description => "Adds an overlay by its ID";
        public string Help => "addoverlay <id>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length != 1)
            {
                shell.SendText(player, "Expected 1 argument.");
                return;
            }

            if (player?.AttachedEntity != null)
            {
                if (player.AttachedEntity.TryGetComponent(out ServerOverlayEffectsComponent overlayEffectsComponent))
                {
                    overlayEffectsComponent.AddOverlay(args[0]);
                }
            }
        }
    }

    [AdminCommand(AdminFlags.Debug)]
    public class RemoveOverlayCommand : IClientCommand
    {
        public string Command => "rmoverlay";
        public string Description => "Removes an overlay by its ID";
        public string Help => "rmoverlay <id>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length != 1)
            {
                shell.SendText(player, "Expected 1 argument.");
                return;
            }

            if (player?.AttachedEntity != null)
            {
                if (player.AttachedEntity.TryGetComponent(out ServerOverlayEffectsComponent overlayEffectsComponent))
                {
                    overlayEffectsComponent.RemoveOverlay(args[0]);
                }
            }
        }
    }
}
