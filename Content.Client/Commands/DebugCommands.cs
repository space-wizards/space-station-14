using Content.Client.GameObjects.EntitySystems;
using Content.Client.Interfaces;
using Content.Shared.GameObjects.Components.Markers;
using Robust.Client.Console.Commands;
using Robust.Client.Interfaces.Console;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Commands
{
    internal sealed class ShowMarkersCommand : IConsoleCommand
    {
        // ReSharper disable once StringLiteralTypo
        public string Command => "showmarkers";
        public string Description => "Toggles visibility of markers such as spawn points.";
        public string Help => "";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            EntitySystem.Get<MarkerSystem>()
                .MarkersVisible ^= true;

            return false;
        }
    }

    internal sealed class ShowWiresCommand : IConsoleCommand
    {
        // ReSharper disable once StringLiteralTypo
        public string Command => "showwires";
        public string Description => "Makes wires always visible.";
        public string Help => "";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            EntitySystem.Get<SubFloorHideSystem>()
                .EnableAll ^= true;

            return false;
        }
    }


    internal sealed class NotifyCommand : IConsoleCommand
    {
        public string Command => "notify";
        public string Description => "Send a notify client side.";
        public string Help => "notify <message>";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var message = args[0];

            var notifyManager = IoCManager.Resolve<IClientNotifyManager>();
            notifyManager.PopupMessage(message);

            return false;
        }
    }

    internal sealed class MappingCommand : IConsoleCommand
    {
        public string Command => "mapping";
        public string Description => "Creates and teleports you to a new uninitialized map for mapping.";
        public string Help => $"Usage: {Command} <id> <mapname>";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            if (args.Length != 2)
            {
                console.AddLine(Help);
                return false;
            }

            console.Commands["togglelight"].Execute(console);
            console.Commands["showwires"].Execute(console);

            return true;
        }
    }
}
