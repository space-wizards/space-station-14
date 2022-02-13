using System;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public class TemperatureArtifactSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TemperatureArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(EntityUid uid, TemperatureArtifactComponent component, ArtifactActivatedEvent args)
    {
        var transform = Transform(uid);

        var environment = _atmosphereSystem.GetTileMixture(transform.Coordinates, true);
        if (environment == null)
            return;

        var dif = component.TargetTemperature - environment.Temperature;
        var absDif = Math.Abs(dif);
        if (absDif < component.MaxTemperatureDifference)
            return;

        var step = Math.Min(absDif, component.SpawnTemperature);
        environment.Temperature += dif > 0 ? step : -step;
    }
}
