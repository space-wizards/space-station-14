using Content.Server.Atmos.EntitySystems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

public sealed class ArtifactGasTriggerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly ArtifactSystem _artifactSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArtifactGasTriggerComponent, RandomizeTriggerEvent>(OnRandomizeTrigger);
    }

    private void OnRandomizeTrigger(EntityUid uid, ArtifactGasTriggerComponent component, RandomizeTriggerEvent args)
    {
        if (component.ActivationGas == null)
        {
            var gas = _random.Pick(component.PossibleGases);
            component.ActivationGas = gas;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityManager.EntityQuery<ArtifactGasTriggerComponent, TransformComponent>();
        foreach (var (trigger, transform) in query)
        {
            var uid = trigger.Owner;

            if (trigger.ActivationGas == null)
                continue;

            var environment = _atmosphereSystem.GetTileMixture(transform.GridUid, transform.MapUid,
                _transformSystem.GetGridOrMapTilePosition(uid, transform));

            if (environment == null)
                continue;

            // check if outside there is enough moles to activate artifact
            var moles = environment.GetMoles(trigger.ActivationGas.Value);
            if (moles < trigger.ActivationMoles)
                continue;

            _artifactSystem.TryActivateArtifact(trigger.Owner);
        }
    }
}
