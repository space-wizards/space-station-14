using Content.Shared.Power.Components;
using JetBrains.Annotations;

namespace Content.Shared.Power.EntitySystems;

/// <summary>
/// Generic system that handles entities with <see cref="PowerStateComponent"/>.
/// Used for simple machines that only need to switch between "idle" and "working" power states.
/// </summary>
public abstract partial class SharedPowerStateSystem : EntitySystem
{
    [Dependency] private SharedPowerReceiverSystem _powerReceiverSystem = default!;

    [Dependency] protected EntityQuery<PowerStateComponent> _powerStateQuery = default!;

    /// <summary>
    /// Sets the working state of the entity, adjusting its power draw accordingly.
    /// </summary>
    /// <param name="ent">The entity to set the working state for.</param>
    /// <param name="working">Whether the entity should be in the working state.</param>
    /// <param name="shouldRaiseEvent">
    /// Should setting state raise event? Can help omitting events during initialization.
    /// </param>
    [PublicAPI]
    public virtual void SetWorkingState(Entity<PowerStateComponent?> ent, bool working, bool shouldRaiseEvent = true)
    {
        if (!_powerStateQuery.Resolve(ent, ref ent.Comp))
            return;

        SharedApcPowerReceiverComponent? apcPower = null;
        if (_powerReceiverSystem.ResolveApc(ent, ref apcPower))
            _powerReceiverSystem.SetLoad((ent, apcPower), working ? ent.Comp.WorkingPowerDraw : ent.Comp.IdlePowerDraw);

        ent.Comp.IsWorking = working;
        if(!shouldRaiseEvent)
            return;

        var ev = new PowerStateChanged(working);
        RaiseLocalEvent(ent, ref ev);
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

    /// <summary>
    /// Tries to get working state out of <see cref="PowerStateComponent"/>,
    /// returns it if found, returns false if not found.
    /// </summary>
    [PublicAPI]
    public bool GetWorkingState(Entity<PowerStateComponent?> ent)
    {
        if (!_powerStateQuery.Resolve(ent, ref ent.Comp))
            return false;

        return ent.Comp.IsWorking;
    }
}

/// <summary>
/// Event of changing power state of entity.
/// </summary>
[ByRefEvent]
public record struct PowerStateChanged(bool IsWorking);
