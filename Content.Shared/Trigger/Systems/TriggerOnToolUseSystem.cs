using Content.Shared.Tools.Components;
using Content.Shared.Trigger.Components.Triggers;

namespace Content.Shared.Trigger.Systems;

public sealed class TriggerOnToolUseSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnSimpleToolUsageComponent, SimpleToolDoAfterEvent>(OnToolUse);
    }

    private void OnToolUse(Entity<TriggerOnSimpleToolUsageComponent> ent, ref SimpleToolDoAfterEvent args)
    {
        _trigger.Trigger(ent.Owner, args.User, ent.Comp.KeyOut);
    }
}
