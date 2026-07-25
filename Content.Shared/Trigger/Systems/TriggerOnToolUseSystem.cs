using Content.Shared.Tools.Components;
using Content.Shared.Trigger.Components.Triggers;

namespace Content.Shared.Trigger.Systems;

public sealed class TriggerOnToolUseSystem : TriggerOnXSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnSimpleToolUsageComponent, SimpleToolDoAfterEvent>(OnToolUse);
    }

    private void OnToolUse(Entity<TriggerOnSimpleToolUsageComponent> ent, ref SimpleToolDoAfterEvent args)
    {
        if (args.Cancelled) return;
        if (args.Cancelled)
            return;

        Trigger.Trigger(ent.Owner, args.User, ent.Comp.KeyOut);
    }
}
