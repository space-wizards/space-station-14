using Content.Server.Atmos.Components;
using Content.Shared.Clothing;
using Content.Shared.Inventory.Events;
using Content.Shared.Inventory;

namespace Content.Server.Body.Systems;

public sealed class SmokeFilterSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FilterMaskComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<FilterMaskComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<FilterMaskComponent, ItemMaskToggledEvent>(OnMaskToggled);
    }

    private void OnGotUnequipped(Entity<FilterMaskComponent> ent, ref GotUnequippedEvent args)
    {
        ent.Comp.IsActive = false;
    }

    private void OnGotEquipped(Entity<FilterMaskComponent> ent, ref GotEquippedEvent args)
    {
        ent.Comp.IsActive = true;

    }

    private void OnMaskToggled(Entity<FilterMaskComponent> ent, ref ItemMaskToggledEvent args)
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

        FilterMaskComponent? filter;

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
