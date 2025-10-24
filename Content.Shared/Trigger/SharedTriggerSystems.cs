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
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target is not { } uid)
            return;

        OnTrigger(ent, uid, ref args);
    }

    protected abstract void OnTrigger(Entity<T> ent, EntityUid target, ref TriggerEvent args);
}
