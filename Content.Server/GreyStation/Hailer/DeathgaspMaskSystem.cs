using Content.Server.Mobs;
using Content.Shared.Clothing;
using Content.Shared.GreyStation.Hailer;

namespace Content.Server.GreyStation.Hailer;

public sealed class DeathgaspMaskSystem : SharedDeathgaspMaskSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeathgaspMaskComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<DeathgaspMaskComponent, ClothingGotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<DeathgaspMaskComponent, ItemMaskToggledEvent>(OnToggled);
    }

    private void OnEquipped(Entity<DeathgaspMaskComponent> ent, ref ClothingGotEquippedEvent args)
    {
        SetEmote(ent.Comp, args.Wearer);
    }

    private void OnUnequipped(Entity<DeathgaspMaskComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        Revert(ent.Comp, args.Wearer);
    }

    private void OnToggled(Entity<DeathgaspMaskComponent> ent, ref ItemMaskToggledEvent args)
    {
        if (args.IsToggled)
            Revert(ent.Comp, args.Wearer);
        else
            SetEmote(ent.Comp, args.Wearer);
    }

    private void Revert(DeathgaspMaskComponent comp, EntityUid user)
    {
        if (comp.PreviousPrototype is not {} previous)
            return;

        if (!TryComp<DeathgaspComponent>(user, out var deathgasp))
            return;

        deathgasp.Prototype = previous;
    }

    private void SetEmote(DeathgaspMaskComponent comp, EntityUid user)
    {
        if (!TryComp<DeathgaspComponent>(user, out var deathgasp))
            return;

        comp.PreviousPrototype = deathgasp.Prototype;
        deathgasp.Prototype = comp.Prototype;
    }
}
