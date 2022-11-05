using Content.Server.Atmos.EntitySystems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;

public sealed class ArtifactGasTriggerSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly ArtifactSystem _artifactSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArtifactGasTriggerComponent, ArtifactNodeEnteredEvent>(OnRandomizeTrigger);
    }

    private void OnRandomizeTrigger(EntityUid uid, ArtifactGasTriggerComponent component, ArtifactNodeEnteredEvent args)
    {
        if (component.ActivationGas != null)
            return;

        var gas = component.PossibleGases[args.RandomSeed % component.PossibleGases.Count];
        component.ActivationGas = gas;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        List<ArtifactComponent> toUpdate = new();
        foreach (var (trigger, artifact, transform) in EntityQuery<ArtifactGasTriggerComponent, ArtifactComponent, TransformComponent>())
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

            toUpdate.Add(artifact);
        }

        foreach (var a in toUpdate)
        {
            _artifactSystem.TryActivateArtifact(a.Owner, null, a);
        }
    }
}
