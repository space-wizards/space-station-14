using System;
using Content.Client.Markers;
using Content.Client.Popups;
using Content.Shared.SubFloor;
using Robust.Client.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.Commands
{
    internal sealed class ShowMarkersCommand : IConsoleCommand
    {
        // ReSharper disable once StringLiteralTypo
        public string Command => "showmarkers";
        public string Description => "Toggles visibility of markers such as spawn points.";
        public string Help => "";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            EntitySystem.Get<MarkerSystem>()
                .MarkersVisible ^= true;
        }
    }

    internal sealed class ShowSubFloor : IConsoleCommand
    {
        // ReSharper disable once StringLiteralTypo
        public string Command => "showsubfloor";
        public string Description => "Makes entities below the floor always visible.";
        public string Help => $"Usage: {Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            EntitySystem.Get<SubFloorHideSystem>()
                .ShowAll ^= true;
        }
    }

    internal sealed class ShowSubFloorForever : IConsoleCommand
    {
        // ReSharper disable once StringLiteralTypo
        public string Command => "showsubfloorforever";
        public string Description => "Makes entities below the floor always visible until the client is restarted.";
        public string Help => $"Usage: {Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            EntitySystem.Get<SubFloorHideSystem>()
                .ShowAll = true;

            var components = IoCManager.Resolve<IEntityManager>()
                .EntityQuery<SubFloorHideComponent>(true);

            foreach (var component in components)
            {
                if (component.Owner.TryGetComponent(out ISpriteComponent? sprite))
                {
                    sprite.DrawDepth = (int) DrawDepth.Overlays;
                }
            }
        }
    }

    internal sealed class NotifyCommand : IConsoleCommand
    {
        public string Command => "notify";
        public string Description => "Send a notify client side.";
        public string Help => "notify <message>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var message = args[0];

            EntitySystem.Get<PopupSystem>().PopupCursor(message);
        }
    }

    internal sealed class MappingCommand : IConsoleCommand
    {
        public string Command => "mapping";
        public string Description => "Creates and teleports you to a new uninitialized map for mapping.";
        public string Help => $"Usage: {Command} <mapname> / {Command} <id> <mapname>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length == 0)
            {
                shell.WriteLine(Help);
                return;
            }

#if DEBUG
            shell.WriteError("WARNING: The client is using a debug build. You are risking losing your changes.");
#endif

            shell.ConsoleHost.RegisteredCommands["togglelight"].Execute(shell, string.Empty, Array.Empty<string>());
            shell.ConsoleHost.RegisteredCommands["showsubfloorforever"].Execute(shell, string.Empty, Array.Empty<string>());

            shell.RemoteExecuteCommand(argStr);
        }
    }
}
