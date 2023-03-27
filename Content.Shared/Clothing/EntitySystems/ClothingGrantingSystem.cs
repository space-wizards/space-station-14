using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Tag;
using Robust.Shared.Serialization.Manager;

namespace Content.Shared.Clothing.EntitySystems;

// modified from simplestation14
public sealed class ClothingGrantingSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingGrantTagComponent, GotEquippedEvent>(OnTagEquip);
        SubscribeLocalEvent<ClothingGrantTagComponent, GotUnequippedEvent>(OnTagUnequip);
    }

    private void OnTagEquip(EntityUid uid, ClothingGrantTagComponent comp, GotEquippedEvent args)
    {
        if (!TryComp<ClothingComponent>(uid, out var clothing) || !clothing.Slots.HasFlag(args.SlotFlags))
            return;

        var tag = EnsureComp<TagComponent>(args.Equipee);
        _tag.AddTag(tag, comp.Tag);

        comp.IsActive = true;
    }

    private void OnTagUnequip(EntityUid uid, ClothingGrantTagComponent comp, GotUnequippedEvent args)
    {
        if (!comp.IsActive) return;

        _tag.RemoveTag(args.Equipee, comp.Tag);

        comp.IsActive = false;
    }
}
