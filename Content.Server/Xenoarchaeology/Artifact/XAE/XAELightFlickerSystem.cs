using Content.Server.Ghost;
using Content.Server.Light.Components;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

public sealed class XAELightFlickerSystem : BaseXAESystem<XAELightFlickerComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAELightFlickerComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var lights = GetEntityQuery<PoweredLightComponent>();
        foreach (var light in _lookup.GetEntitiesInRange(ent.Owner, ent.Comp.Radius, LookupFlags.StaticSundries))
        {
            if (!lights.HasComponent(light))
                continue;

            if (!_random.Prob(ent.Comp.FlickerChance))
                continue;

            //todo: extract effect from ghost system, update power system accordingly
            _ghost.DoGhostBooEvent(light);
        }
    }
}
