using Content.Client.GameObjects.Components;
using Content.Client.GameObjects.EntitySystems;
using Content.Client.Interfaces;
using Content.Shared.GameObjects;
using Robust.Client.Interfaces.Console;
using Robust.Client.Interfaces.GameObjects.Components;
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

    internal sealed class ShowSubFloor : IConsoleCommand
    {
        // ReSharper disable once StringLiteralTypo
        public string Command => "showsubfloor";
        public string Description => "Makes entities below the floor always visible.";
        public string Help => $"Usage: {Command}";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            EntitySystem.Get<SubFloorHideSystem>()
                .EnableAll ^= true;

            return false;
        }
    }

    internal sealed class ShowSubFloorForever : IConsoleCommand
    {
        // ReSharper disable once StringLiteralTypo
        public string Command => "showsubfloorforever";
        public string Description => "Makes entities below the floor always visible until the client is restarted.";
        public string Help => $"Usage: {Command}";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            EntitySystem.Get<SubFloorHideSystem>()
                .EnableAll = true;

            var components = IoCManager.Resolve<IEntityManager>().ComponentManager
                .EntityQuery<SubFloorHideComponent>();

            foreach (var component in components)
            {
                if (component.Owner.TryGetComponent(out ISpriteComponent sprite))
                {
                    sprite.DrawDepth = (int) DrawDepth.Overlays;
                }
            }

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
        public string Help => $"Usage: {Command} <mapname> / {Command} <id> <mapname>";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            if (args.Length == 0)
            {
                console.AddLine(Help);
                return false;
            }

            console.Commands["togglelight"].Execute(console);
            console.Commands["showsubfloorforever"].Execute(console);

            return true;
        }
    }
}
