using System;
using System.Collections.Generic;
using Content.Server.Solar.Components;
using Content.Server.UserInterface;
using Content.Shared.Radar;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.Radar;

public class RadarConsoleSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        foreach (var component in EntityManager.EntityQuery<RadarConsoleComponent>())
        {
            var s = component.Owner.GetUIOrNull(RadarConsoleUiKey.Key);
            var radarTransform = Transform(component.Owner);
            var radarPos = radarTransform.WorldPosition;
            var radarRot = radarTransform.WorldRotation;

            if (s is null)
                continue;

            var map = Transform(component.Owner).MapID;

            var objects = new List<RadarObjectData>();

            foreach (var grid in _mapManager.GetAllMapGrids(map))
            {
                var phy = Comp<PhysicsComponent>(grid.GridEntityId);
                var transform = Transform(grid.GridEntityId);
                if (phy.Mass < 50)
                    continue;
                var rad = Math.Log10(phy.Mass);
                var pos = radarTransform.InvWorldMatrix.Transform(transform.WorldPosition);
                pos.Y = -pos.Y; // Robust has an inverted Y, like BYOND. This undoes that.
                if (pos.Length > 256)
                    continue;
                objects.Add(new RadarObjectData() {Color = Color.Aqua, Position = pos, Radius = (float)rad});
            }

            s.SetState(new RadarConsoleBoundInterfaceState(objects.ToArray(), radarPos));
        }
    }
}
