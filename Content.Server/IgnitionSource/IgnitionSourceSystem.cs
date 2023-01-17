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
    private HashSet<EntityUid> _ignitionSources = new();

    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<IgnitionSourceComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<IgnitionSourceComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<IgnitionSourceComponent, ComponentShutdown>(OnShutdown);
    }

    public void OnStartup(EntityUid uid, IgnitionSourceComponent component, ComponentStartup args)
    {
        _ignitionSources.Add(uid);
    }

    public void SetState(EntityUid uid, IgnitionSourceComponent component, bool newState)
    {
        component.State = newState;
    }

    public void OnMapInit(EntityUid uid, IgnitionSourceComponent component, MapInitEvent args)
    {
        _ignitionSources.Add(uid);
    }

    public void OnShutdown(EntityUid uid, IgnitionSourceComponent component, ComponentShutdown args)
    {
        _ignitionSources.Remove(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var source in _ignitionSources.ToArray())
        {
            if (!EntityManager.TryGetComponent(source, out IgnitionSourceComponent? component)
                || !EntityManager.TryGetComponent(source, out TransformComponent? transform))
                continue;

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
