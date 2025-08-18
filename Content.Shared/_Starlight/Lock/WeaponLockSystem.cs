using Content.Shared.Popups;
using Content.Shared.Lock;
using Content.Shared.Hands;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared.Starlight.Lock;

public partial class WeaponLockSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<LockComponent, AttemptShootEvent>(OnShootAttempt);
        SubscribeLocalEvent<LockComponent, GotUnequippedHandEvent>(OnUnequipHand);
        SubscribeLocalEvent<LockComponent, GotEquippedHandEvent>(OnEquipHand);
    }
    
    private void OnShootAttempt(EntityUid uid, LockComponent component, ref AttemptShootEvent args)
    {
        if (component.Locked)
        {
            args.Cancelled = true;
            _popup.PopupEntity(Loc.GetString("lock-comp-weapon-locked"), uid, args.User, PopupType.MediumCaution);
        }
    }
    
    private void OnUnequipHand(EntityUid uid, LockComponent component, GotUnequippedHandEvent args)
    {
        if (component.AutoUnlock)
            _lock.Lock(uid, null, component);
    }
    
    private void OnEquipHand(EntityUid uid, LockComponent component, GotEquippedHandEvent args)
    {
        if (component.AutoLock)
            _lock.TryUnlock(uid, args.User, component);
    }
}