using Content.Server.Speech.Components;
using Content.Shared.Clothing;
using Content.Shared.Cuffs;

namespace Content.Server.Speech.EntitySystems;

public sealed class AddAccentClothingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AddAccentClothingComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<AddAccentClothingComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<AddAccentClothingComponent, CuffsAppliedEvent>(OnCuffed);
        SubscribeLocalEvent<AddAccentClothingComponent, CuffsRemovedEvent>(OnUncuffed);
    }

    //  TODO: Turn this into a relay event.
    private void OnGotEquipped(Entity<AddAccentClothingComponent> ent, ref ClothingGotEquippedEvent args)
    {
        AddAccent(ent, args.Wearer);
    }

    private void OnGotUnequipped(Entity<AddAccentClothingComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        RemoveAccent(ent, args.Wearer);
    }

    private void OnCuffed(Entity<AddAccentClothingComponent> ent, ref CuffsAppliedEvent args)
    {
        AddAccent(ent, args.Target);
    }

    private void OnUncuffed(Entity<AddAccentClothingComponent> ent, ref CuffsRemovedEvent args)
    {
        RemoveAccent(ent, args.Target);
    }

    private void AddAccent(Entity<AddAccentClothingComponent> ent, EntityUid target)
    {
        // does the user already has this accent?
        var componentType = Factory.GetRegistration(ent.Comp.Accent).Type;
        if (HasComp(target, componentType))
            return;

        // add accent to the user
        var accentComponent = (Component) Factory.GetComponent(componentType);
        AddComp(target, accentComponent);

        // snowflake case for replacement accent
        if (accentComponent is ReplacementAccentComponent rep)
            rep.Accent = ent.Comp.ReplacementPrototype!;

        ent.Comp.IsActive = true;
    }

    private void RemoveAccent(Entity<AddAccentClothingComponent> ent, EntityUid target)
    {
        if (!ent.Comp.IsActive)
            return;

        // try to remove accent
        var componentType = Factory.GetRegistration(ent.Comp.Accent).Type;
        RemComp(target, componentType);

        ent.Comp.IsActive = false;
    }
}
