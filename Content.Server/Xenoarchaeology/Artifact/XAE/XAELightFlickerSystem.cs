using Content.Server.Ghost;
using Content.Server.Light.Components;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for xeno artifact activation effect that flickers light on and off.
/// </summary>
public sealed class XAELightFlickerSystem : BaseXAESystem<XAELightFlickerComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;

    private EntityQuery<PoweredLightComponent> _lights;

    /// <summary> Pre-allocated and re-used collection.</summary>
    private readonly HashSet<EntityUid> _entities = new();

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        _lights = GetEntityQuery<PoweredLightComponent>();
    }

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAELightFlickerComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        _entities.Clear();
        _lookup.GetEntitiesInRange(ent.Owner, ent.Comp.Radius, _entities, LookupFlags.StaticSundries);
        foreach (var light in _entities)
        {
            if (!_lights.HasComponent(light))
                continue;

            if (!_random.Prob(ent.Comp.FlickerChance))
                continue;

            //todo: extract effect from ghost system, update power system accordingly
            _ghost.DoGhostBooEvent(light);
        }
    }
}
