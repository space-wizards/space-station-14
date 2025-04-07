using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Examine;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Security.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Security.Systems;

public abstract class SharedGenpopSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly MetaDataSystem MetaDataSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GenpopLockerComponent, LockToggleAttemptEvent>(OnLockToggleAttempt);
        SubscribeLocalEvent<GenpopLockerComponent, LockToggledEvent>(OnLockToggled);
        SubscribeLocalEvent<GenpopIdCardComponent, ExaminedEvent>(OnExamine);
    }

    private void OnLockToggleAttempt(Entity<GenpopLockerComponent> ent, ref LockToggleAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.LinkedId == null)
        {
            args.Cancelled = true;
            return;
        }

        // Make sure that we both have the linked ID on our person AND the Id has actually expired.
        // That way, even if someone escapes early, they can't get ahold of their things.
        if (!_access.FindPotentialAccessItems(args.User).Contains(ent.Comp.LinkedId.Value))
        {
            if (!args.Silent)
                _popup.PopupClient(Loc.GetString("lock-comp-has-user-access-fail"), ent, args.User);
            args.Cancelled = true;
            return;
        }

        if (!TryComp<ExpireIdCardComponent>(ent.Comp.LinkedId.Value, out var expireIdCard) ||
            !expireIdCard.Expired)
        {
            if (!args.Silent)
                _popup.PopupClient(Loc.GetString("genpop-prisoner-id-popup-not-served"), ent, args.User);
            args.Cancelled = true;
        }
    }

    private void OnLockToggled(Entity<GenpopLockerComponent> ent, ref LockToggledEvent args)
    {
        if (args.Locked)
            return;

        // If we unlock the door, then we're gonna reset the ID.
        CancelIdCard(ent);
    }

    private void CancelIdCard(Entity<GenpopLockerComponent> ent)
    {
        if (ent.Comp.LinkedId == null)
            return;

        var metaData = MetaData(ent);
        MetaDataSystem.SetEntityName(ent, Loc.GetString("genpop-locker-name-default"), metaData);
        MetaDataSystem.SetEntityDescription(ent, Loc.GetString("genpop-locker-desc-default"), metaData);

        ent.Comp.LinkedId = null;
        Dirty(ent);
    }

    private void OnExamine(Entity<GenpopIdCardComponent> ent, ref ExaminedEvent args)
    {
        // This component holds the contextual data for the sentence end time and other such things.
        if (!TryComp<ExpireIdCardComponent>(ent, out var expireIdCard))
            return;

        if (expireIdCard.Permanent)
        {
            args.PushText(Loc.GetString("genpop-prisoner-id-examine-wait-perm",
                ("crime", ent.Comp.Crime)));
        }
        else
        {
            if (expireIdCard.Expired)
            {
                args.PushText(Loc.GetString("genpop-prisoner-id-examine-served",
                    ("crime", ent.Comp.Crime)));
            }
            else
            {
                var sentence = expireIdCard.ExpireTime - ent.Comp.StartTime;
                var remaining = expireIdCard.ExpireTime - Timing.CurTime;

                args.PushText(Loc.GetString("genpop-prisoner-id-examine-wait",
                    ("minutes", remaining.Minutes),
                    ("seconds", remaining.Seconds),
                    ("sentence", sentence.TotalMinutes),
                    ("crime", ent.Comp.Crime)));
            }
        }
    }
}
