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


    public override void Initialize()
    {
        // ? does this need to exist
    }


    public void SetState(EntityUid uid, IgnitionSourceComponent component, bool newState)
    {
        component.State = newState;
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
                    SetState(source, component, true);

                if (expendable.CurrentState == ExpendableLightState.BrandNew)
                    SetState(source, component, false);

                if (!component.State)
                    continue;
            }

            if (transform.GridUid is { } gridUid)
            {
                var position = _transformSystem.GetGridOrMapTilePosition(source, transform);
                _atmosphereSystem.HotspotExpose(gridUid, position, 700, 50, true);
            }
        }
    }
}

