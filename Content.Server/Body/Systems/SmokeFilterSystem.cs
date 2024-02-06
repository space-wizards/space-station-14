using Content.Server.Atmos.Components;
using Content.Shared.Clothing;
using Content.Shared.Inventory.Events;
using Content.Shared.Inventory;

namespace Content.Server.Body.Systems;
public sealed class SmokeFilterystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SmokeFilterComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<SmokeFilterComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<SmokeFilterComponent, ItemMaskToggledEvent>(OnMaskToggled);
    }

    private void OnGotUnequipped(EntityUid uid, SmokeFilterComponent component, GotUnequippedEvent args)
    {
        component.IsActive = false;
    }

    private void OnGotEquipped(EntityUid uid, SmokeFilterComponent component, GotEquippedEvent args)
    {
        component.IsActive = true;

    }

    private void OnMaskToggled(Entity<SmokeFilterComponent> ent, ref ItemMaskToggledEvent args)
    {
        if (args.IsToggled || args.IsEquip)
        {
            ent.Comp.IsActive = false;
        }
        else
        {
            ent.Comp.IsActive = true;
        }
    }


    public bool AreFilterWorking(EntityUid uid)
    {

        SmokeFilterComponent? filter;

        if (_inventory.TryGetSlotEntity(uid, "mask", out var maskUid) &&
            TryComp(maskUid, out filter) &&
            filter.IsActive)
        {
            return true;
        }

        if (_inventory.TryGetSlotEntity(uid, "head", out var headUid) &&
            TryComp(headUid, out filter) &&
            filter.IsActive)
        {
            return true;
        }
        return false;
    }

}
