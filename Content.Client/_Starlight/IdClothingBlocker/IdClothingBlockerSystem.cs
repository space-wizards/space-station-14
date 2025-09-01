using Content.Client.Popups;
using Content.Shared._Starlight.IdClothingBlocker;
using Content.Shared.Inventory.Events;
using Content.Shared.DoAfter;
using Content.Shared.Clothing.Components;
using Content.Shared.Popups;

namespace Content.Client._Starlight.IdClothingBlocker;

public sealed class IdClothingBlockerSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<IdClothingBlockerComponent, BeingUnequippedAttemptEvent>(OnClientUnequipAttempt);
        SubscribeLocalEvent<IdClothingBlockerComponent, DoAfterAttemptEvent<ClothingUnequipDoAfterEvent>>(OnClientUnequipDoAfterAttempt);
    }
    
    private void OnClientUnequipAttempt(EntityUid uid, IdClothingBlockerComponent component, BeingUnequippedAttemptEvent args)
    {
        if (EntityManager.TryGetComponent<IdClothingFrozenComponent>(args.Unequipee, out var frozenComponent))
        {
            if (frozenComponent.IsBlocked && frozenComponent.ClothingItem == uid)
            {
                args.Cancel();
                _popup.PopupEntity(Loc.GetString("access-clothing-blocker-notify-unauthorized-access"), args.Unequipee, PopupType.MediumCaution);
            }
        }
    }
    
    private void OnClientUnequipDoAfterAttempt(EntityUid uid, IdClothingBlockerComponent component, DoAfterAttemptEvent<ClothingUnequipDoAfterEvent> args)
    {
        if (args.DoAfter.Args.Target == null)
            return;
            
        if (EntityManager.TryGetComponent<IdClothingFrozenComponent>(args.DoAfter.Args.Target.Value, out var frozenComponent))
        {
            if (frozenComponent.IsBlocked && frozenComponent.ClothingItem == uid)
            {
                args.Cancel();
                _popup.PopupEntity(Loc.GetString("access-clothing-blocker-notify-unauthorized-access"), args.DoAfter.Args.Target.Value, PopupType.MediumCaution);
            }
        }
    }
} 