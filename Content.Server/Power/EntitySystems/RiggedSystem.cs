using Content.Server.Chemistry.EntitySystems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Power.Components;

namespace Content.Server.Power.EntitySystems;

/// <summary>
///  Handles sabotaged/rigged objects
/// </summary>
public sealed class RiggedSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionsSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;

    public void IsRigged(EntityUid uid, Component comp, string solutionName, SolutionChangedEvent args)
    {
        if (TryComp<RiggedComponent>(uid, out var riggedComp))
        {
            riggedComp.IsRigged = _solutionsSystem.TryGetSolution(uid, solutionName, out var solution)
                                 && solution.TryGetReagent("Plasma", out var plasma)
                                 && plasma >= 5;
        }
    }

    public void Explode(EntityUid uid, BatteryComponent? battery = null, EntityUid? cause = null)
    {
        if (!Resolve(uid, ref battery))
            return;

        var radius = MathF.Min(5, MathF.Sqrt(battery.CurrentCharge) / 9);

        _explosionSystem.TriggerExplosive(uid, radius: radius, user:cause);
        QueueDel(uid);
    }
}
