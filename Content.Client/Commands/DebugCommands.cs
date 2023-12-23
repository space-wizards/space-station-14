using Content.Client.Markers;
using Content.Client.Popups;
using Content.Client.SubFloor;
using Content.Shared.SubFloor;
using Robust.Client.GameObjects;
using Robust.Shared.Console;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.Commands
{
    internal sealed class ShowMarkersCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        // ReSharper disable once StringLiteralTypo
        public string Command => "showmarkers";
        public string Description => Loc.GetString("debug-command-show-markers-description");
        public string Help => Loc.GetString("debug-command-show-markers-help", ("command", Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            _entitySystemManager.GetEntitySystem<MarkerSystem>().MarkersVisible ^= true;
        }
    }

    internal sealed class ShowSubFloor : IConsoleCommand
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        // ReSharper disable once StringLiteralTypo
        public string Command => "showsubfloor";
        public string Description => Loc.GetString("debug-command-show-sub-floor-description");
        public string Help => Loc.GetString("debug-command-show-sub-floor-help", ("command", Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            _entitySystemManager.GetEntitySystem<SubFloorHideSystem>().ShowAll ^= true;
        }
    }

    internal sealed class ShowSubFloorForever : IConsoleCommand
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        // ReSharper disable once StringLiteralTypo
        public string Command => "showsubfloorforever";
        public string Description => Loc.GetString("debug-command-show-sub-floor-forever-description");
        public string Help => Loc.GetString("debug-command-show-sub-floor-forever-help", ("command", Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            _entitySystemManager.GetEntitySystem<SubFloorHideSystem>().ShowAll = true;

            var entMan = IoCManager.Resolve<IEntityManager>();
            var components = entMan.EntityQuery<SubFloorHideComponent, SpriteComponent>(true);

            foreach (var (_, sprite) in components)
            {
                sprite.DrawDepth = (int) DrawDepth.Overlays;
            }
        }
    }

    internal sealed class NotifyCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        public string Command => "notify";
        public string Description => Loc.GetString("debug-command-notify-description");
        public string Help => Loc.GetString("debug-command-notify-help", ("command", Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var message = args[0];

            _entitySystemManager.GetEntitySystem<PopupSystem>().PopupCursor(message);
        }
    }
}
