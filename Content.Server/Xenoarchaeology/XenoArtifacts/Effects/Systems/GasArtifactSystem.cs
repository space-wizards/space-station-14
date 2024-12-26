using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Atmos;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class GasArtifactSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasArtifactComponent, ArtifactNodeEnteredEvent>(OnNodeEntered);
        SubscribeLocalEvent<GasArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnNodeEntered(EntityUid uid, GasArtifactComponent component, ArtifactNodeEnteredEvent args)
    {
        if (component.SpawnGas == null && component.PossibleGases.Count != 0)
        {
            var gas = component.PossibleGases[args.RandomSeed % component.PossibleGases.Count];
            component.SpawnGas = gas;
        }

        if (component.SpawnTemperature == null)
        {
            var temp = args.RandomSeed % component.MaxRandomTemperature - component.MinRandomTemperature +
                       component.MinRandomTemperature;
            component.SpawnTemperature = temp;
        }
    }

    private void OnActivate(EntityUid uid, GasArtifactComponent component, ArtifactActivatedEvent args)
    {
        if (component.SpawnGas == null || component.SpawnTemperature == null)
            return;

        var environment = _atmosphereSystem.GetContainingMixture(uid, false, true);
        if (environment == null)
            return;

        if (environment.Pressure >= component.MaxExternalPressure)
            return;

        var merger = new GasMixture(1) { Temperature = component.SpawnTemperature.Value };
        merger.SetMoles(component.SpawnGas.Value, component.SpawnAmount);

        _atmosphereSystem.Merge(environment, merger);
    }
}
