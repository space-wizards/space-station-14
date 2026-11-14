using Content.Shared.Power.Components;
using JetBrains.Annotations;

namespace Content.Shared.Power.EntitySystems;

/// <summary>
/// Generic system that handles entities with <see cref="PowerStateComponent"/>.
/// Used for simple machines that only need to switch between "idle" and "working" power states.
/// </summary>
public abstract partial class PowerStateSystem : EntitySystem
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

        if (ent.Comp.IsTurnedOn == isWorking)
            return;

        ent.Comp.IsTurnedOn = isWorking;

        var powerStateEnt = (ent, ent.Comp);
        var hasPower = SetPowerLoadGetHasPower(powerStateEnt, isWorking);
        
        var ev = new TurnedOnStateChanged(isWorking);
        RaiseLocalEvent(ent, ref ev);

        SetIsPoweredState(powerStateEnt, hasPower);
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

        return ent.Comp.IsTurnedOn;
    }

    /// <summary>
    /// Updates appearance according to powered situation.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnApcChanged(Entity<PowerStateComponent> ent, ref PowerChangedEvent args)
    {
        if (!ent.Comp.IsTurnedOn)
            return;

        SetIsPoweredState(ent, args.Powered);
    }

    /// <summary>
    /// Sets up power load for provided working state and return if device is sufficiently powered.
    /// </summary>
    protected virtual bool SetPowerLoadGetHasPower(Entity<PowerStateComponent> ent, bool isWorking)
    {
        SharedApcPowerReceiverComponent? apcPower = null;
        if (_powerReceiverSystem.ResolveApc(ent, ref apcPower))
        {
            var powerLoadToSet = isWorking ? ent.Comp.WorkingPowerDraw : ent.Comp.IdlePowerDraw;
            var apcPowerEnt = (ent, apcPower);
            _powerReceiverSystem.SetLoad(apcPowerEnt, powerLoadToSet);
            // this will not react to power load instantly, but will mark if device is powered at-all.
            // Could provide one-tick move before powered will be updated
            // and PoweredChangedEvent is going to shut device down due to shortage
            return _powerReceiverSystem.IsPowered(apcPowerEnt);
        }

        return false;
    }

    /// <summary>
    /// Set `powered` flag, update appearance and send event if needed.
    /// </summary>
    /// <param name="ent">Entity to update.</param>
    /// <param name="hasPower">Is entity getting enough power to operate properly?</param>
    protected void SetIsPoweredState(Entity<PowerStateComponent> ent, bool hasPower)
    {
        UpdateAppearance(ent, hasPower);

        var isPowered = ent.Comp.IsTurnedOn && hasPower;
        if (ent.Comp.IsOperating == isPowered)
            return;

        ent.Comp.IsOperating = isPowered;
        Dirty(ent);

        var ev = new OperatingStateChanged(isPowered);
        RaiseLocalEvent(ent, ref ev);
    }

    /// <summary>
    /// Updates flags for visual state according to <see cref="PowerStateComponent.IsTurnedOn"/>
    /// and <see cref="hasPower"/> flags (making 3 states of <see cref="PowerStateDeviceVisualState"/>
    /// as a result).
    /// </summary>
    /// <param name="ent">Entity that requires visual state update.</param>
    /// <param name="hasPower">Is device sufficiently powered?</param>
    protected void UpdateAppearance(Entity<PowerStateComponent> ent, bool hasPower)
    {
        PowerStateDeviceVisualState state;
        if (hasPower && ent.Comp.IsTurnedOn)
            state = PowerStateDeviceVisualState.On;
        else if (ent.Comp.IsTurnedOn)
            state = PowerStateDeviceVisualState.Underpowered;
        else
            state = PowerStateDeviceVisualState.Off;

        _appearance.SetData(ent, PowerStateDeviceVisuals.VisualState, state);
    }
}

/// <summary>
/// Event of changing power state of entity.
/// </summary>
[ByRefEvent]
public record struct TurnedOnStateChanged(bool IsTurnedOn);

/// <summary>
/// Event of changing if device is operating properly - power demand is met, and it is turned ON.
/// </summary>
[ByRefEvent]
public record struct OperatingStateChanged(bool IsOperating);
