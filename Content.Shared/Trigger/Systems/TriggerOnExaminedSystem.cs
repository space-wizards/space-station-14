using Content.Shared.Examine;
using Content.Shared.Trigger.Components.Triggers;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerOnExaminedSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnExaminedComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<TriggerOnExaminedComponent> ent, ref ExaminedEvent args)
    {
        _trigger.Trigger(ent.Owner, args.Examiner, ent.Comp.KeyOut);
    }
}
