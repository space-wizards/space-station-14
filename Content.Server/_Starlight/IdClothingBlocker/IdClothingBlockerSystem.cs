using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Shared._Starlight.IdClothingBlocker;
using Content.Server.Access.Components;
using Content.Shared.Access.Components;
using Content.Shared.PDA;
using Content.Shared.Access.Systems;
using Content.Shared.Clothing.Components;
using Content.Shared.DoAfter;
using Content.Shared.Inventory.Events;

namespace Content.Server._Starlight.IdClothingBlocker;

public sealed class IdClothingBlockerSystem : SharedIdClothingBlockerSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    
    public override void Initialize()
    {
        base.Initialize();
    }

    protected override async void OnUnauthorizedAccess(EntityUid clothingUid, IdClothingBlockerComponent component, EntityUid wearer)
    {
        var blockedComponent = EntityManager.EnsureComponent<IdClothingFrozenComponent>(wearer);
        blockedComponent.ClothingItem = clothingUid;
        SetBlocked(clothingUid, component, true);
        Dirty(wearer, blockedComponent);

        _popup.PopupEntity(Loc.GetString("access-clothing-blocker-notify-unauthorized-access"), clothingUid, PopupType.MediumCaution);
    }

    public void SetBlocked(EntityUid uid, IdClothingBlockerComponent component, bool blocked)
    {
        component.IsBlocked = blocked;
        Dirty(uid, component);
    }

    protected override void PopupClient(string message, EntityUid uid, EntityUid? target = null)
    {
        if (target.HasValue)
        {
            _popup.PopupEntity(message, uid, target.Value, PopupType.MediumCaution);
        }
    }

    protected override void OnUnequipAttempt(EntityUid uid, IdClothingBlockerComponent component, BeingUnequippedAttemptEvent args)
    {
        var wearerHasAccess = HasJobAccess(args.Unequipee, component);
        if (wearerHasAccess)
            return;

        if (args.UnEquipTarget == args.Unequipee)
        {
            args.Cancel();
        }
    }

    protected override void OnUnequipDoAfterAttempt(EntityUid uid, IdClothingBlockerComponent component, DoAfterAttemptEvent<ClothingUnequipDoAfterEvent> args)
    {
        if (args.DoAfter.Args.Target == null)
            return;

        var wearerHasAccess = HasJobAccess(args.DoAfter.Args.Target.Value, component);

        if (wearerHasAccess)
            return;

        args.Cancel();
        PopupClient(Loc.GetString("access-clothing-blocker-notify-unauthorized-access"), uid);
    }

    protected override bool HasJobAccess(EntityUid wearer, IdClothingBlockerComponent component)
    {
        if (component.AllowedJobs == null)
            return true;

        if (!_accessReader.FindAccessItemsInventory(wearer, out var items))
        {
            return false;
        }

        foreach (var item in items)
        {
            if (TryComp<PresetIdCardComponent>(item, out var preset))
            {
                if (preset.JobName != null && component.AllowedJobs.Contains(preset.JobName))
                {
                    return true;
                }
            }

            // ID Card
            if (TryComp<IdCardComponent>(item, out var id))
            {
                if (id.JobPrototype != null && component.AllowedJobs.Contains(id.JobPrototype.Value))
                {
                    return true;
                }
            }

            // PDA
            if (TryComp<PdaComponent>(item, out var pda))
            {
                if (pda.ContainedId != null)
                {
                    if (TryComp(pda.ContainedId, out preset))
                    {
                        if (preset.JobName != null && component.AllowedJobs.Contains(preset.JobName))
                        {
                            return true;
                        }
                    }
                    
                    if (TryComp(pda.ContainedId, out id))
                    {
                        if (id.JobPrototype != null && component.AllowedJobs.Contains(id.JobPrototype.Value))
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }
}
