using Content.Shared.Tag;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed class AddTagsOnTriggerSystem : XOnTriggerSystem<AddTagsOnTriggerComponent>
{
    [Dependency] private readonly TagSystem _tag = default!;

    protected override void OnTrigger(Entity<AddTagsOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        _tag.AddTags(target, ent.Comp.Tags);
        args.Handled = true;
    }
}

public sealed class RemoveTagsOnTriggerSystem : XOnTriggerSystem<RemoveTagsOnTriggerComponent>
{
    [Dependency] private readonly TagSystem _tag = default!;

    protected override void OnTrigger(Entity<RemoveTagsOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        _tag.RemoveTags(target, ent.Comp.Tags);
        args.Handled = true;
    }
}
