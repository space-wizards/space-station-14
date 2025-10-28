using Content.Shared.Trigger.Components.Conditions;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Trigger.Systems;

namespace Content.Shared.Trigger;

/// <summary>
/// This is a base Trigger system which handles all the boilerplate for triggers automagically!
/// </summary>
public abstract class TriggerOnXSystem : EntitySystem
{
    [Dependency] protected readonly TriggerSystem Trigger = default!;
}

/// <summary>
/// This is a base Trigger system which handles all the boilerplate for triggers automagically!
/// </summary>
public abstract class XOnTriggerSystem<T> : EntitySystem where T : BaseXOnTriggerComponent
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<T> ent, ref TriggerEvent args)
    {
        if (args.Keys != null && !ent.Comp.KeysIn.Overlaps(args.Keys))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target is not { } uid)
            return;

        OnTrigger(ent, uid, ref args);
    }

    protected abstract void OnTrigger(Entity<T> ent, EntityUid target, ref TriggerEvent args);
}

/// <summary>
/// This is a base Trigger system which handles (almost) all the boilerplate for trigger conditions automagically!
/// </summary>
public abstract class TriggerConditionSystem<T> : EntitySystem where T : BaseTriggerConditionComponent
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, AttemptTriggerEvent>(OnAttempt);
    }

    private void OnAttempt(Entity<T> ent, ref AttemptTriggerEvent ev)
    {
        // If the trigger is already cancelled we only need to run checks if this condition wants to add a cancel trigger
        if (ev.Cancelled && ent.Comp.CancelKeyOut == null)
            return;

        // Does the condition care about this trigger attempt
        if (ev.Key == null || !ent.Comp.Keys.Contains(ev.Key))
            return;

        CheckCondition(ent, ref ev);
    }

    /// <summary>
    /// Individual condition logic goes here. MUST call <see cref="ModifyEvent"/>.
    /// </summary>
    protected abstract void CheckCondition(Entity<T> ent, ref AttemptTriggerEvent args);

    /// <summary>
    /// Method placed at the end of <see cref="CheckCondition"/> to modify the event.
    /// </summary>
    /// <param name="result">What the condition evaluated to.</param>
    protected void ModifyEvent(Entity<T> ent, bool result, ref AttemptTriggerEvent ev)
    {
        if (ent.Comp.Inverted)
            result = !result;

        // Only add the key to the cancel trigger if this condition (not another condition) would cancel it
        if (result && ent.Comp.CancelKeyOut != null)
            ev.CancelKeys.Add(ent.Comp.CancelKeyOut);

        // Bitwise operation to assign true, but won't overwrite with false if it was set to true somewhere else
        ev.Cancelled |= result;
    }
}
