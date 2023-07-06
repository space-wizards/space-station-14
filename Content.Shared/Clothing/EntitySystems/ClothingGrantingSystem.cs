using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;
using Robust.Shared.Serialization.Manager;
using System.Linq;

namespace Content.Shared.Clothing;

public sealed class ClothingGrantingSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ClothingGrantComponentComponent, GotEquippedEvent>(OnCompEquip);
        SubscribeLocalEvent<ClothingGrantComponentComponent, GotUnequippedEvent>(OnCompUnequip);
    }

    private void OnCompEquip(EntityUid uid, ClothingGrantComponentComponent component, GotEquippedEvent args)
    {
        if (!TryComp<ClothingComponent>(uid, out var clothing)) return;

        if (!clothing.Slots.HasFlag(args.SlotFlags)) return;

        component.IsActive = true;

        foreach (var (name, data) in component.Components)
        {
            var newComp = (Component) _componentFactory.GetComponent(name);

            if (HasComp(args.Equipee, newComp.GetType()))
                continue;

            newComp.Owner = args.Equipee;

            var temp = (object) newComp;
            _serializationManager.CopyTo(data.Component, ref temp);
            EntityManager.AddComponent(args.Equipee, (Component) temp!);
        }
    }

    private void OnCompUnequip(EntityUid uid, ClothingGrantComponentComponent component, GotUnequippedEvent args)
    {
        if (!component.IsActive) return;

        foreach (var (name, data) in component.Components)
        {
            var newComp = (Component) _componentFactory.GetComponent(name);

            var allGrantComponents = EntityManager.GetComponents<ClothingGrantComponentComponent>(args.Equipee);

            if (allGrantComponents.Where(x => x!.IsActive).Count(x => x!.Components.ContainsKey(name)) <= 1)
            {
                RemComp(args.Equipee, newComp.GetType());
            }
        }

        component.IsActive = false;
    }
}
