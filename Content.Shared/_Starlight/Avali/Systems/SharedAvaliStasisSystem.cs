using Content.Shared.Actions;
using Content.Shared.Starlight.Avali.Components;

namespace Content.Shared.Starlight.Avali.Systems;

/// <summary>
/// Allows mobs to enter nanite induced stasis <see cref="AvaliStasisComponent"/>.
/// </summary>
public abstract class SharedAvaliStasisSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AvaliStasisComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AvaliStasisComponent, ComponentShutdown>(OnCompRemove);
        SubscribeLocalEvent<AvaliStasisComponent, AvaliPrepareStasisActionEvent>(OnPrepareStasisStart);
        SubscribeLocalEvent<AvaliStasisComponent, AvaliEnterStasisActionEvent>(OnEnterStasisStart);
        SubscribeLocalEvent<AvaliStasisComponent, AvaliExitStasisActionEvent>(OnExitStasisStart);
    }

    /// <summary>
    /// Giveths the action to preform stasis on the entity
    /// </summary>
    protected virtual void OnMapInit(EntityUid uid, AvaliStasisComponent comp, MapInitEvent args)
    {
    }

    /// <summary>
    /// Takeths away the action to preform stasis from the entity.
    /// </summary>
    protected virtual void OnCompRemove(EntityUid uid, AvaliStasisComponent comp, ComponentShutdown args)
    {
    }

    protected virtual void OnPrepareStasisStart(EntityUid uid, AvaliStasisComponent comp,
        AvaliPrepareStasisActionEvent args)
    {
    }
    
    protected virtual void OnEnterStasisStart(EntityUid uid, AvaliStasisComponent comp,
        AvaliEnterStasisActionEvent args)
    {
    }

    protected virtual void OnExitStasisStart(EntityUid uid, AvaliStasisComponent comp, AvaliExitStasisActionEvent args)
    {
    }
}

/// <summary>
/// Should be relayed upon using the action.
/// </summary>
public sealed partial class AvaliPrepareStasisActionEvent : InstantActionEvent
{
}

/// <summary>
/// Should be relayed preparation to stasis being complete.
/// </summary>
public sealed partial class AvaliEnterStasisActionEvent : InstantActionEvent
{
}

/// <summary>
/// Should be relayed upon using the action.
/// </summary>
public sealed partial class AvaliExitStasisActionEvent : InstantActionEvent
{
}