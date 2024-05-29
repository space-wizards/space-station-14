using Content.Shared.Popups;
using Content.Shared.Whitelist;

namespace Content.Shared.Lock;

public sealed class LockingWhitelistSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LockingWhitelistComponent, LockToggleAttemptEvent>(OnLockToggleAttempt);
    }

    private void OnLockToggleAttempt(Entity<LockingWhitelistComponent> ent, ref LockToggleAttemptEvent args)
    {
        if (_whitelistSystem.CheckBoth(args.Target, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;

        if (!args.Silent)
            _popupSystem.PopupClient(Loc.GetString("locking-whitelist-component-lock-toggle-deny"), ent.Owner);

        args.Cancelled = true;
    }
}
