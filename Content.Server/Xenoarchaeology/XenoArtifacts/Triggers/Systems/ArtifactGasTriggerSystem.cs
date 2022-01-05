using Content.Server.Atmos.EntitySystems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

public class ArtifactGasTriggerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly ArtifactSystem _artifactSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArtifactGasTriggerComponent, ComponentInit>(OnInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityManager.EntityQuery<ArtifactGasTriggerComponent>();
        foreach (var component in query)
        {
            if (component.ActivationGas == null)
                return;

            var transform = Transform(component.Owner);
            var environment = _atmosphereSystem.GetTileMixture(transform.Coordinates, true);

            if (environment == null)
                return;

            // check if outside there is enough moles to activate artifact
            var moles = environment.GetMoles(component.ActivationGas.Value);
            if (moles < component.ActivationMoles)
                return;

            _artifactSystem.TryActivateArtifact(component.Owner);
        }
    }

    private void OnInit(EntityUid uid, ArtifactGasTriggerComponent component, ComponentInit args)
    {
        if (component.RandomGas)
        {
            var gas = _random.Pick(component.PossibleGases);
            component.ActivationGas = gas;
        }
    }
}
