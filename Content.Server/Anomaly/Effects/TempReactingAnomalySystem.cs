using Content.Server.Atmos.EntitySystems;
using Content.Server.Anomaly.Components;
using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Anomaly.Effects;

/// <summary>
/// This handles <see cref="TempReactingAnomalyComponent"/>
/// </summary>
public sealed class TempReactingAnomalySystem : EntitySystem
{
    [Dependency] private readonly SharedAnomalySystem _anomalySystem = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<TempReactingAnomalyComponent, AnomalyPulseEvent>(OnPulse);
    }

    private void OnPulse(EntityUid uid, TempReactingAnomalyComponent tempReactingAnomaly, ref AnomalyPulseEvent args)
    {
        var tf = Transform(uid);
        var grid = tf.GridUid;
        var map = tf.MapUid;
        var indices = _transformSystem.GetGridOrMapTilePosition(uid, tf);
        var mixture = _atmosphereSystem.GetTileMixture(grid, map, indices);

        if (mixture is null)
            return;

        foreach (var reaction in tempReactingAnomaly.Reactions)
        {
            if (reaction.TempInRange(mixture.Temperature) && TryComp<AnomalyComponent>(uid, out var anomaly))
            {
                _anomalySystem.ChangeAnomalyStability(uid, reaction.StabilityPerPulse, anomaly);
                _anomalySystem.ChangeAnomalySeverity(uid, reaction.SeverityPerPulse, anomaly);
                _anomalySystem.ChangeAnomalyHealth(uid, reaction.HealthPerPulse, anomaly);
            }
        }
    }
}
