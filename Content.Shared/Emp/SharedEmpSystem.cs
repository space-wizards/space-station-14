using Content.Shared.Examine;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Shared.Emp;

public abstract class SharedEmpSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmpDisabledComponent, ExaminedEvent>(OnExamine);
    }

    protected const string EmpDisabledEffectPrototype = "EffectEmpDisabled";

    /// <summary>
    /// Triggers an EMP pulse at the given location, by first raising an <see cref="EmpAttemptEvent"/>, then a raising <see cref="EmpPulseEvent"/> on all entities in range.
    /// </summary>
    /// <param name="coordinates">The location to trigger the EMP pulse at.</param>
    /// <param name="range">The range of the EMP pulse.</param>
    /// <param name="energyConsumption">The amount of energy consumed by the EMP pulse.</param>
    /// <param name="duration">The duration of the EMP effects.</param>
    public virtual void EmpPulse(MapCoordinates coordinates, float range, float energyConsumption, float duration)
    {
    }

    private void OnExamine(Entity<EmpDisabledComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("emp-disabled-comp-on-examine"));
    }
}
