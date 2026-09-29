using Content.Server.Atmos.EntitySystems;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Atmos;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for xeno artifact effect that changes atmospheric temperature on adjacent tiles.
/// </summary>
public sealed partial class XAETemperatureSystem : BaseXAESystem<XAETemperatureComponent>
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAETemperatureComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        XAETemperatureComponent component = ent;
        var adjacentTileEffectProbability = component.AdjacentTileEffectProbability;
        if (args.Modifications.TryGetValue(XenoArtifactEffectModifier.Range, out var chanceModifier))
        {
            adjacentTileEffectProbability = Math.Max(0.1f, chanceModifier.Modify(adjacentTileEffectProbability));
        }

        var targetTemperature = ent.Comp.TargetTemperature;
        if (args.Modifications.TryGetValue(XenoArtifactEffectModifier.Power, out var modifier))
        {
            targetTemperature = Math.Max(targetTemperature / 8, modifier.Modify(targetTemperature));
        }

        var center = _atmosphereSystem.GetContainingMixture(ent.Owner, false, true);
        if (center == null)
            return;

        UpdateTileTemperature(component, targetTemperature, center);

        var transform = Transform(ent);

        if (adjacentTileEffectProbability > 0 && transform.GridUid != null)
        {
            var position = _transformSystem.GetGridOrMapTilePosition(ent, transform);
            var enumerator = _atmosphereSystem.GetAdjacentTileMixtures(transform.GridUid.Value, position, excite: true);

            while (enumerator.MoveNext(out var mixture))
            {
                if(_random.Prob(adjacentTileEffectProbability))
                    UpdateTileTemperature(component, targetTemperature, mixture);
            }
        }
    }

    private void UpdateTileTemperature(
        XAETemperatureComponent component,
        float targetTemperature,
        GasMixture environment
    )
    {
        var dif = targetTemperature - environment.Temperature;
        var absDif = Math.Abs(dif);
        var step = Math.Min(absDif, component.SpawnTemperature);
        environment.Temperature += dif > 0 ? step : -step;
    }
}
