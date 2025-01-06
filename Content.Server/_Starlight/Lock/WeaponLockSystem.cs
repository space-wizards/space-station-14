using Content.Shared.Popups;
using Content.Shared.Lock;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Server.Starlight.Lock;

public partial class WeaponLockSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<LockComponent, AttemptShootEvent>(OnShootAttempt);
    }
    
    private void OnShootAttempt(EntityUid uid, LockComponent component, ref AttemptShootEvent args)
    {
        if (component.Locked)
        {
            args.Cancelled = true;
            _popup.PopupEntity(Loc.GetString("lock-comp-weapon-locked"), uid, args.User, PopupType.MediumCaution);
        }
    }
}