using Content.Shared.Power.Components;
using JetBrains.Annotations;

namespace Content.Shared.Power.EntitySystems;

/// <summary>
/// Generic system that handles entities with <see cref="PowerStateComponent"/>.
/// Used for simple machines that only need to switch between "idle" and "working" power states.
/// </summary>
public abstract class SharedPowerStateSystem : EntitySystem
{
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiverSystem = default!;

    private EntityQuery<PowerStateComponent> _powerStateQuery;

    public override void Initialize()
    {
        base.Initialize();

        _powerStateQuery = GetEntityQuery<PowerStateComponent>();
    }

    /// <summary>
    /// Sets the working state of the entity, adjusting its power draw accordingly.
    /// </summary>
    /// <param name="ent">The entity to set the working state for.</param>
    /// <param name="working">Whether the entity should be in the working state.</param>
    [PublicAPI]
    public void SetWorkingState(Entity<PowerStateComponent?> ent, bool working)
    {
        if (!_powerStateQuery.Resolve(ent, ref ent.Comp))
            return;

        _powerReceiverSystem.SetLoad(ent.Owner, working ? ent.Comp.WorkingPowerDraw : ent.Comp.IdlePowerDraw);
        ent.Comp.IsWorking = working;
    }

    /// <summary>
    /// Tries to set the working state of the entity, adjusting its power draw accordingly.
    /// Use this for if you're not sure if the entity has a <see cref="PowerStateComponent"/>.
    /// </summary>
    /// <param name="ent">The entity to set the working state for.</param>
    /// <param name="working">Whether the entity should be in the working state.</param>
    [PublicAPI]
    public void TrySetWorkingState(Entity<PowerStateComponent?> ent, bool working)
    {
        // Sometimes systems calling this API handle generic objects that can or can't consume power,
        // so to reduce boilerplate we don't log an error. Any entity that *should* have an ApcPowerRecieverComponent
        // will log an error in tests if someone tries to add an entity that doesn't have one.
        if (!_powerStateQuery.Resolve(ent, ref ent.Comp, false))
            return;

        SetWorkingState(ent, working);
    }
}
