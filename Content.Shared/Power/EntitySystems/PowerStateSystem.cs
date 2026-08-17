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
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    [Dependency] private EntityQuery<PowerStateComponent> _powerStateQuery;

    /// <summary>
    /// Sets the working state of the entity, adjusting its power draw accordingly.
    /// </summary>
    /// <param name="ent">The entity to set the working state for.</param>
    /// <param name="isWorking">Whether the entity should be in the working state.</param>
    [PublicAPI]
    public virtual void SetWorkingState(Entity<PowerStateComponent?> ent, bool isWorking)
    {
        if (!_powerStateQuery.Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.IsWorking = isWorking;

        var powerStateEnt = (ent, ent.Comp);
        var isPowered = TrySetPowerLoad(powerStateEnt, isWorking);
        UpdateAppearance(powerStateEnt, isPowered);

        var ev = new PowerStateChanged(isWorking);
        RaiseLocalEvent(ent, ref ev);
    }

    /// <summary>
    /// Tries to set the working state of the entity, adjusting its power draw accordingly.
    /// Use this for if you're not sure if the entity has a <see cref="PowerStateComponent"/>.
    /// </summary>
    /// <param name="ent">The entity to set the working state for.</param>
    /// <param name="isWorking">Whether the entity should be in the working state.</param>
    [PublicAPI]
    public void TrySetWorkingState(Entity<PowerStateComponent?> ent, bool isWorking)
    {
        // Sometimes systems calling this API handle generic objects that can or can't consume power,
        // so to reduce boilerplate we don't log an error. Any entity that *should* have an ApcPowerRecieverComponent
        // will log an error in tests if someone tries to add an entity that doesn't have one.
        if (!_powerStateQuery.Resolve(ent, ref ent.Comp, false))
            return;

        SetWorkingState(ent, isWorking);
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

    /// <summary> Sets up power load for provided working state. </summary>
    protected virtual bool TrySetPowerLoad(Entity<PowerStateComponent> ent, bool isWorking)
    {
        SharedApcPowerReceiverComponent? apcPower = null;
        if (_powerReceiverSystem.ResolveApc(ent, ref apcPower))
        {
            var powerLoadToSet = isWorking ? ent.Comp.WorkingPowerDraw : ent.Comp.IdlePowerDraw;
            _powerReceiverSystem.SetLoad((ent, apcPower), powerLoadToSet);
            return apcPower.Powered;
        }

        return false;
    }

    protected void UpdateAppearance(Entity<PowerStateComponent> ent, bool isPowered)
    {
        PowerStateDeviceVisualState state;
        if (isPowered)
        {
            state = PowerStateDeviceVisualState.On;
        }
        else if (ent.Comp.IsWorking)
        {
            state = PowerStateDeviceVisualState.Underpowered;
        }
        else
        {
            state = PowerStateDeviceVisualState.Off;
        }
        _appearance.SetData(ent, PowerStateDeviceVisuals.VisualState, state);
    }
}

/// <summary>
/// Event of changing power state of entity.
/// </summary>
[ByRefEvent]
public record struct PowerStateChanged(bool IsWorking);
