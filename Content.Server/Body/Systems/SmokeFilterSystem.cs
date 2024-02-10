using Content.Server.Atmos.Components;
using Content.Shared.Clothing;
using Content.Shared.Inventory.Events;
using Content.Shared.Inventory;
using Content.Shared.FixedPoint;
using Content.Shared.Examine;

namespace Content.Server.Body.Systems;

public sealed class SmokeFilterSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FilterMaskComponent, ExaminedEvent>(OnMaskExamine);
        SubscribeLocalEvent<FilterMaskComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<FilterMaskComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<FilterMaskComponent, ItemMaskToggledEvent>(OnMaskToggled);
        SubscribeLocalEvent<FilterWorkingEvent>(UseFilter);
    }

    private void OnGotUnequipped(Entity<FilterMaskComponent> ent, ref GotUnequippedEvent args)
    {
        ent.Comp.IsActive = false;
    }

    private void OnGotEquipped(Entity<FilterMaskComponent> ent, ref GotEquippedEvent args)
    {
        if (ent.Comp.State > 0)
            ent.Comp.IsActive = true;
    }

    private void OnMaskToggled(Entity<FilterMaskComponent> ent, ref ItemMaskToggledEvent args)
    {
        if (args.IsToggled || args.IsEquip || ent.Comp.State <= 0)
        {
            ent.Comp.IsActive = false;
        }
        else
        {
            ent.Comp.IsActive = true;
        }
    }


    private void UseFilter(ref FilterWorkingEvent args)
    {
        EntityUid uid = args.Uid;
        FilterMaskComponent? filter;
        if (_inventory.TryGetSlotEntity(uid, "mask", out var maskUid) &&
            TryComp(maskUid, out filter) &&
            filter.IsActive)
        {
            if (filter.State <= 1)
            {
                filter.IsActive = false;
                return;
            }
            args.IsActive = true;
            args.Particlepass = filter.Particle_ignore;
            filter.State = filter.State - filter.Use_rate;
            return;
        }

        if (_inventory.TryGetSlotEntity(uid, "head", out var headUid) &&
            TryComp(headUid, out filter) &&
            filter.IsActive)
        {
            if (filter.State <= 0)
            {
                filter.IsActive = false;
                return;
            }
            args.IsActive = true;
            args.Particlepass = filter.Particle_ignore;
            filter.State = filter.State - filter.Use_rate;
        }
    }



    private void OnMaskExamine(Entity<FilterMaskComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(FilterMaskComponent)))
        {
            if (args.IsInDetailsRange)
            {
                var state = ent.Comp.State;
                if (state <= 0)
                {
                    args.PushMarkup(Loc.GetString("filter-mask-component-on-examine-detailed-message-destroyed"));
                    return;
                }
                if (state > ent.Comp.Good_State)
                {
                    args.PushMarkup(Loc.GetString("filter-mask-component-on-examine-detailed-message-good"));
                    return;
                }
                if (state >= ent.Comp.Bad_State)
                {
                    args.PushMarkup(Loc.GetString("filter-mask-component-on-examine-detailed-message-medium"));
                    return;
                }
                if (state < ent.Comp.Bad_State)
                {
                    args.PushMarkup(Loc.GetString("filter-mask-component-on-examine-detailed-message-bad"));
                    return;
                }

            }
        }
    }
}


[ByRefEvent]
public record struct FilterWorkingEvent(EntityUid Uid, bool IsActive = false, FixedPoint2? Particlepass = null);
