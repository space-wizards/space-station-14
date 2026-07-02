using Content.Shared.Tag;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed partial class AddTagsOnTriggerSystem : XOnTriggerSystem<AddTagsOnTriggerComponent>
{
    [Dependency] private TagSystem _tag = default!;

    protected override void OnTrigger(Entity<AddTagsOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        _tag.AddTags(target, ent.Comp.Tags);
        args.Handled = true;
    }
}

public sealed partial class RemoveTagsOnTriggerSystem : XOnTriggerSystem<RemoveTagsOnTriggerComponent>
{
    [Dependency] private TagSystem _tag = default!;

    protected override void OnTrigger(Entity<RemoveTagsOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        _tag.RemoveTags(target, ent.Comp.Tags);
        args.Handled = true;
    }
}
