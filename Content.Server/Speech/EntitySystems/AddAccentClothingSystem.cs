using Content.Server.Speech.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;

namespace Content.Server.Speech.EntitySystems;

public sealed class AddAccentClothingSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AddAccentClothingComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<AddAccentClothingComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(EntityUid uid, AddAccentClothingComponent component, GotEquippedEvent args)
    {
        if (!TryComp(uid, out ClothingComponent? clothing))
            return;

        // check if entity was actually used as clothing
        // not just taken in pockets or something
        var isCorrectSlot = clothing.Slots.HasFlag(args.SlotFlags);
        if (!isCorrectSlot) return;

        // does the user already has this accent?
        var componentType = _componentFactory.GetRegistration(component.Accent).Type;
        if (EntityManager.HasComponent(args.Equipee, componentType)) return;

        // add accent to the user
        var accentComponent = (Component) _componentFactory.GetComponent(componentType);
        accentComponent.Owner = args.Equipee;
        EntityManager.AddComponent(args.Equipee, accentComponent);

        // snowflake case for replacement accent
        if (accentComponent is ReplacementAccentComponent rep)
            rep.Accent = component.ReplacementPrototype!;

        component.IsActive = true;
    }

    private void OnGotUnequipped(EntityUid uid, AddAccentClothingComponent component, GotUnequippedEvent args)
    {
        if (!component.IsActive)
            return;

        // try to remove accent
        var componentType = _componentFactory.GetRegistration(component.Accent).Type;
        if (EntityManager.HasComponent(args.Equipee, componentType))
        {
            EntityManager.RemoveComponent(args.Equipee, componentType);
        }

        component.IsActive = false;
    }
}
