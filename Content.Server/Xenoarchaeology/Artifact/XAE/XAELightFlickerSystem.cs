using Content.Server.Ghost;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Light.Components;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for xeno artifact activation effect that flickers light on and off.
/// </summary>
public sealed partial class XAELightFlickerSystem : BaseXAESystem<XAELightFlickerComponent>
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private GhostSystem _ghost = default!;
    [Dependency] private EntityQuery<PoweredLightComponent> _poweredLightsQuery = default!;

    /// <summary> Pre-allocated and re-used collection.</summary>
    private readonly HashSet<EntityUid> _entities = new();

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAELightFlickerComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        _entities.Clear();
        _lookup.GetEntitiesInRange(ent.Owner, ent.Comp.Radius, _entities, LookupFlags.StaticSundries);
        foreach (var light in _entities)
        {
            if (!_poweredLightsQuery.HasComponent(light))
                continue;

            if (!_random.Prob(ent.Comp.FlickerChance))
                continue;

            //todo: extract effect from ghost system, update power system accordingly
            _ghost.DoGhostBooEvent(light);
        }
    }
}
