using System.Diagnostics;
using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Light.Components;
using Content.Shared.Light.Component;
using Robust.Server.GameObjects;
using Serilog;

namespace Content.Server.IgnitionSource;

/// <summary>
/// This handles ignition
/// </summary>
public sealed class IgnitionSourceSystem : EntitySystem
{
    /// <inheritdoc/>
    ///

    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    private void SetIgnited(EntityUid uid, IgnitionSourceComponent component, bool newState)
    {
        component.Ignited = newState;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);


        foreach (var (component,transform) in EntityQuery<IgnitionSourceComponent,TransformComponent>())
        {
            var source = component.Owner;

            if(EntityManager.TryGetComponent(source, out ExpendableLightComponent? expendable))
            {
                if (expendable.CurrentState != ExpendableLightState.Dead)
                    SetIgnited(source, component, true);

                if (expendable.CurrentState == ExpendableLightState.BrandNew)
                    SetIgnited(source, component, false);

                if (!component.Ignited)
                    continue;
            }

            if (transform.GridUid is { } gridUid)
            {
                var position = _transformSystem.GetGridOrMapTilePosition(source, transform);
                _atmosphereSystem.HotspotExpose(gridUid, position, component.Temperature, 50, true);
            }
        }
    }
}
