using Content.Shared.Mobs.Systems;
using Content.Shared.Tag;
using Content.Shared.Trigger.Components.StepTriggers;
using Content.Shared.Whitelist;

namespace Content.Shared.Trigger.Systems;

/// <summary>
/// Handles "Attempt" components that works with <see cref="TriggerStepAttemptEvent"/> and continues or cancels stepAttempt.
/// </summary>
public sealed class TriggerOnStepTriggerAttemptSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnStepAliveAttemptComponent, TriggerStepAttemptEvent>(OnAliveAttempt);
        SubscribeLocalEvent<TriggerOnStepAlwaysAttemptComponent, TriggerStepAttemptEvent>(OnAlwaysAttempt);
        SubscribeLocalEvent<TriggerOnStepTagAttemptComponent, TriggerStepAttemptEvent>(OnTagAttempt);
        SubscribeLocalEvent<TriggerOnStepWhitelistAttemptComponent, TriggerStepAttemptEvent>(OnWhitelistAttempt);
    }

    private void OnAliveAttempt(Entity<TriggerOnStepAliveAttemptComponent> ent, ref TriggerStepAttemptEvent args)
    {
        if (args.Continue || args.Cancelled)
            return;

        args.Continue = _mobState.IsAlive(ent);
        args.Cancelled = CheckIsCancellable(ent.Comp, args.Continue);
    }

    private void OnAlwaysAttempt(Entity<TriggerOnStepAlwaysAttemptComponent> ent, ref TriggerStepAttemptEvent args)
    {
        if (args.Continue || args.Cancelled)
            return;

        args.Continue = true;
    }

    private void OnTagAttempt(Entity<TriggerOnStepTagAttemptComponent> ent, ref TriggerStepAttemptEvent args)
    {
        if (args.Continue || args.Cancelled)
            return;

        if (ent.Comp.RequiredTags is null
            || ent.Comp.RequiredTags.Count == 0
            || _tag.HasAllTags(args.Tripper, ent.Comp.RequiredTags))
            args.Continue = true;

        args.Cancelled = CheckIsCancellable(ent.Comp, args.Continue);
    }

    private void OnWhitelistAttempt(Entity<TriggerOnStepWhitelistAttemptComponent> ent, ref TriggerStepAttemptEvent args)
    {
        if (args.Continue || args.Cancelled)
            return;

        if (_whitelist.IsWhitelistPassOrNull(ent.Comp.Whitelist, args.Tripper)
            && _whitelist.IsWhitelistFailOrNull(ent.Comp.Whitelist, args.Tripper))
            args.Continue = true;

        args.Cancelled = CheckIsCancellable(ent.Comp, args.Continue);
    }

    /// <summary>
    /// Checks Cancellable is on, and then returns opposite of continued.
    /// </summary>
    /// <param name="component">Trigger that inherits BaseStepTriggerOnXComponent.</param>
    /// <returns></returns>
    private bool CheckIsCancellable(BaseStepTriggerOnXComponent component, bool continued)
    {
        if (component.Cancellable)
            return !continued;

        return false;
    }
}
