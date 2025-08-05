using Content.Shared.Actions;

namespace Content.Shared._Starlight.Actions.Stasis;

/// <summary>
/// Allows mobs to enter nanite induced stasis <see cref="StasisComponent"/>.
/// </summary>
public abstract class SharedStasisSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StasisComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StasisComponent, ComponentShutdown>(OnCompRemove);
        SubscribeLocalEvent<StasisComponent, PrepareStasisActionEvent>(OnPrepareStasisStart);
        SubscribeLocalEvent<StasisComponent, EnterStasisActionEvent>(OnEnterStasisStart);
        SubscribeLocalEvent<StasisComponent, ExitStasisActionEvent>(OnExitStasisStart);
    }

    /// <summary>
    /// Giveths the action to preform stasis on the entity
    /// </summary>
    protected virtual void OnMapInit(EntityUid uid, StasisComponent comp, MapInitEvent args)
    {
    }

    /// <summary>
    /// Takeths away the action to preform stasis from the entity.
    /// </summary>
    protected virtual void OnCompRemove(EntityUid uid, StasisComponent comp, ComponentShutdown args)
    {
    }

    protected virtual void OnPrepareStasisStart(EntityUid uid, StasisComponent comp,
        PrepareStasisActionEvent args)
    {
    }

    protected virtual void OnEnterStasisStart(EntityUid uid, StasisComponent comp,
        EnterStasisActionEvent args)
    {
    }

    protected virtual void OnExitStasisStart(EntityUid uid, StasisComponent comp, ExitStasisActionEvent args)
    {
    }
}

/// <summary>
/// Should be relayed upon using the action.
/// </summary>
public sealed partial class PrepareStasisActionEvent : InstantActionEvent
{
}

/// <summary>
/// Should be relayed preparation to stasis being complete.
/// </summary>
public sealed partial class EnterStasisActionEvent : InstantActionEvent
{
}

/// <summary>
/// Should be relayed upon using the action.
/// </summary>
public sealed partial class ExitStasisActionEvent : InstantActionEvent
{
}
