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

public sealed class RadarConsoleSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;

    private static float Frequency = 1.5f;

    private float _accumulator;

    public override void Update(float frameTime)
    {
        _accumulator += frameTime;

        if (_accumulator < Frequency)
            return;

        _accumulator -= Frequency;

        foreach (var (component, xform) in EntityManager.EntityQuery<RadarConsoleComponent, TransformComponent>())
        {
            var s = component.Owner.GetUIOrNull(RadarConsoleUiKey.Key);

            if (s is null)
                continue;

            var (radarPos, _, radarInvMatrix) = xform.GetWorldPositionRotationInvMatrix();

            var mapId = xform.MapID;
            var objects = new List<RadarObjectData>();

            _mapManager.FindGridsIntersectingEnumerator(mapId, new Box2(radarPos - component.Range, radarPos + component.Range), out var enumerator, true);

            while (enumerator.MoveNext(out var grid))
            {
                var phy = Comp<PhysicsComponent>(grid.GridEntityId);
                var transform = Transform(grid.GridEntityId);

                if (phy.Mass < 50)
                    continue;

                var rad = Math.Log2(phy.Mass);
                var gridCenter = transform.WorldMatrix.Transform(phy.LocalCenter);

                var pos = radarInvMatrix.Transform(gridCenter);
                pos.Y = -pos.Y; // Robust has an inverted Y, like BYOND. This undoes that.

                if (pos.Length > component.Range)
                    continue;

                objects.Add(new RadarObjectData {Color = Color.Aqua, Position = pos, Radius = (float)rad});
            }

            s.SetState(new RadarConsoleBoundInterfaceState(component.Range, objects.ToArray()));
        }
    }
}
