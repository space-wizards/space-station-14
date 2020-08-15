using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Markers;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.Administration
{
    public class WarpCommand : IClientCommand
    {
        public string Command => "warp";
        public string Description => "Teleports you to predefined areas on the map.";

        public string Help =>
            "warp <location>\nLocations you can teleport to are predefined by the map. " +
            "You can specify '?' as location to get a list of valid locations.";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (player == null)
            {
                shell.SendText((IPlayerSession) null, "Only players can use this command");
                return;
            }

            if (args.Length != 1)
            {
                shell.SendText(player, "Expected a single argument.");
                return;
            }

            var comp = IoCManager.Resolve<IComponentManager>();
            var location = args[0];
            if (location == "?")
            {
                var locations = string.Join(", ",
                    comp.EntityQuery<WarpPointComponent>()
                        .Select(p => p.Location)
                        .Where(p => p != null)
                        .OrderBy(p => p)
                        .Distinct());

                shell.SendText(player, locations);
            }
            else
            {
                if (player.Status != SessionStatus.InGame || player.AttachedEntity == null)
                {
                    shell.SendText(player, "You are not in-game!");
                    return;
                }

                var mapManager = IoCManager.Resolve<IMapManager>();
                var currentMap = player.AttachedEntity.Transform.MapID;
                var currentGrid = player.AttachedEntity.Transform.GridID;

                var found = comp.EntityQuery<WarpPointComponent>()
                    .Where(p => p.Location == location)
                    .Select(p => p.Owner.Transform.GridPosition)
                    .OrderBy(p => p, Comparer<GridCoordinates>.Create((a, b) =>
                    {
                        // Sort so that warp points on the same grid/map are first.
                        // So if you have two maps loaded with the same warp points,
                        // it will prefer the warp points on the map you're currently on.
                        if (a.GridID == b.GridID)
                        {
                            return 0;
                        }

                        if (a.GridID == currentGrid)
                        {
                            return -1;
                        }

                        if (b.GridID == currentGrid)
                        {
                            return 1;
                        }

                        var mapA = mapManager.GetGrid(a.GridID).ParentMapId;
                        var mapB = mapManager.GetGrid(b.GridID).ParentMapId;

                        if (mapA == mapB)
                        {
                            return 0;
                        }

                        if (mapA == currentMap)
                        {
                            return -1;
                        }

                        if (mapB == currentMap)
                        {
                            return 1;
                        }

                        return 0;
                    }))
                    .FirstOrDefault();

                if (found.GridID != GridId.Invalid)
                {
                    player.AttachedEntity.Transform.GridPosition = found;
                    if (player.AttachedEntity.TryGetComponent(out ICollidableComponent collidable))
                    {
                        foreach (var vcon in collidable.GetControllers())
                        {
                            vcon.Stop();
                        }
                    }
                }
                else
                {
                    shell.SendText(player, "That location does not exist!");
                }
            }
        }
    }
}
