using Content.Server.Radiation;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Robust.Shared.GameObjects;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class RadiateArtifactSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadiateArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(EntityUid uid, RadiateArtifactComponent component, ArtifactActivatedEvent args)
    {
        var transform = Transform(uid);

        var pulseUid = EntityManager.SpawnEntity(component.PulsePrototype, transform.Coordinates);
        if (!TryComp(pulseUid, out RadiationPulseComponent? pulse))
            return;

        pulse.DoPulse();
    }
}
