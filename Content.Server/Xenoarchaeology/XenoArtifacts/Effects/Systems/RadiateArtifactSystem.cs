using Content.Server.Radiation;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Robust.Shared.GameObjects;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public class RadiateArtifactSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadiateArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(EntityUid uid, RadiateArtifactComponent component, ArtifactActivatedEvent args)
    {
        var transform = Transform(uid);

        var pulse = EntityManager.SpawnEntity("RadiationPulse", transform.Coordinates);
        EntityManager.GetComponent<RadiationPulseComponent>(pulse).DoPulse();
    }
}
