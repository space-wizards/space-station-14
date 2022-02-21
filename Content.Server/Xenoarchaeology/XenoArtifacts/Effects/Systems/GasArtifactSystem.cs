using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class GasArtifactSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasArtifactComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GasArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnMapInit(EntityUid uid, GasArtifactComponent component, MapInitEvent args)
    {
        if (component.SpawnGas == null && component.PossibleGases.Length != 0)
        {
            var gas = _random.Pick(component.PossibleGases);
            component.SpawnGas = gas;
        }

        if (component.SpawnTemperature == null)
        {
            var temp = _random.NextFloat(component.MinRandomTemperature, component.MaxRandomTemperature);
            component.SpawnTemperature = temp;
        }
    }

    private void OnActivate(EntityUid uid, GasArtifactComponent component, ArtifactActivatedEvent args)
    {
        if (component.SpawnGas == null || component.SpawnTemperature == null)
            return;

        var transform = Transform(uid);

        var environment = _atmosphereSystem.GetTileMixture(transform.Coordinates, true);
        if (environment == null)
            return;

        if (environment.Pressure >= component.MaxExternalPressure)
            return;

        var merger = new GasMixture(1) { Temperature = component.SpawnTemperature.Value };
        merger.SetMoles(component.SpawnGas.Value, component.SpawnAmount);

        _atmosphereSystem.Merge(environment, merger);
    }
}
