using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Security.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Shared.Security.Systems;

public abstract class SharedGenpopSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly SharedEntityStorageSystem _entityStorage = default!;
    [Dependency] protected readonly SharedIdCardSystem IdCard = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] protected readonly MetaDataSystem MetaDataSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    // CCvar.
    private int _maxIdJobLength;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GenpopLockerComponent, GenpopLockerIdConfiguredMessage>(OnIdConfigured);
        SubscribeLocalEvent<GenpopLockerComponent, StorageCloseAttemptEvent>(OnCloseAttempt);
        SubscribeLocalEvent<GenpopLockerComponent, LockToggleAttemptEvent>(OnLockToggleAttempt);
        SubscribeLocalEvent<GenpopLockerComponent, LockToggledEvent>(OnLockToggled);
        SubscribeLocalEvent<GenpopLockerComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<GenpopIdCardComponent, ExaminedEvent>(OnExamine);

        Subs.CVar(_cfgManager, CCVars.MaxIdJobLength, value => _maxIdJobLength = value, true);
    }

    private void OnIdConfigured(Entity<GenpopLockerComponent> ent, ref GenpopLockerIdConfiguredMessage args)
    {
        // validation.
        if (string.IsNullOrWhiteSpace(args.Name) || args.Name.Length > _maxIdJobLength ||
            args.Sentence < 0 ||
            string.IsNullOrWhiteSpace(args.Crime) || args.Crime.Length > GenpopLockerComponent.MaxCrimeLength)
        {
            return;
        }

        if (!_accessReader.IsAllowed(args.Actor, ent))
            return;

        // We don't spawn the actual ID now because then the locker would eat it.
        // Instead, we just fill in the spot temporarily til the checks pass.
        ent.Comp.LinkedId = EntityUid.Invalid;

        _lock.Lock(ent.Owner, null);
        _entityStorage.CloseStorage(ent);

        CreateId(ent, args.Name, args.Sentence, args.Crime);
    }

    private void OnCloseAttempt(Entity<GenpopLockerComponent> ent, ref StorageCloseAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        // We cancel no matter what. Our second option is just opening the closet.
        if (ent.Comp.LinkedId == null)
        {
            args.Cancelled = true;
        }

        if (args.User is not { } user)
            return;

        if (!_accessReader.IsAllowed(user, ent))
        {
            _popup.PopupClient(Loc.GetString("lock-comp-has-user-access-fail"), user);
            return;
        }

        // my heart yearns for this to be predicted but for some reason opening an entitystorage via
        // verb does not predict it properly.
        _userInterface.TryOpenUi(ent.Owner, GenpopLockerUiKey.Key, user);
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

        // Make sure that we both have the linked ID on our person AND the ID has actually expired.
        // That way, even if someone escapes early, they can't get ahold of their things.
        if (!_accessReader.FindPotentialAccessItems(args.User).Contains(ent.Comp.LinkedId.Value))
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

    private void OnGetVerbs(Entity<GenpopLockerComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (ent.Comp.LinkedId == null)
            return;

        if (!args.CanAccess || !args.CanComplexInteract || !args.CanInteract)
            return;

        if (!TryComp<ExpireIdCardComponent>(ent.Comp.LinkedId, out var expire) ||
            !TryComp<GenpopIdCardComponent>(ent.Comp.LinkedId, out var genpopId))
            return;

        var user = args.User;
        var hasAccess = _accessReader.IsAllowed(args.User, ent);
        args.Verbs.Add(new Verb // End sentence early.
        {
            Act = () =>
            {
                IdCard.ExpireId((ent.Comp.LinkedId.Value, expire));
            },
            Priority = 13,
            Text = Loc.GetString("genpop-locker-action-end-early"),
            Impact = LogImpact.Medium,
            DoContactInteraction = true,
            Disabled = !hasAccess,
        });

        args.Verbs.Add(new Verb // Cancel Sentence.
        {
            Act = () =>
            {
                CancelIdCard(ent, user);
            },
            Priority = 12,
            Text = Loc.GetString("genpop-locker-action-clear-id"),
            Impact = LogImpact.Medium,
            DoContactInteraction = true,
            Disabled = !hasAccess,
        });

        var servedTime = 1 - (expire.ExpireTime - Timing.CurTime).TotalSeconds / genpopId.SentenceDuration.TotalSeconds;

        // Can't reset it after its expired.
        if (expire.Expired)
            return;

        args.Verbs.Add(new Verb // Reset Sentence.
        {
            Act = () =>
            {
                IdCard.SetExpireTime((ent.Comp.LinkedId.Value, expire), Timing.CurTime + genpopId.SentenceDuration);
            },
            Priority = 11,
            Text = Loc.GetString("genpop-locker-action-reset-sentence", ("percent", Math.Clamp(servedTime, 0, 1) * 100)),
            Impact = LogImpact.Medium,
            DoContactInteraction = true,
            Disabled = !hasAccess,
        });
    }

    private void CancelIdCard(Entity<GenpopLockerComponent> ent, EntityUid? user = null)
    {
        if (ent.Comp.LinkedId == null)
            return;

        var metaData = MetaData(ent);
        MetaDataSystem.SetEntityName(ent, Loc.GetString("genpop-locker-name-default"), metaData);
        MetaDataSystem.SetEntityDescription(ent, Loc.GetString("genpop-locker-desc-default"), metaData);

        ent.Comp.LinkedId = null;
        _lock.Unlock(ent.Owner, user);
        _entityStorage.OpenStorage(ent.Owner);

        if (TryComp<ExpireIdCardComponent>(ent.Comp.LinkedId, out var expire))
            IdCard.ExpireId((ent.Comp.LinkedId.Value, expire));

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
                var sentence = ent.Comp.SentenceDuration;
                var served = ent.Comp.SentenceDuration - (expireIdCard.ExpireTime - Timing.CurTime);

                args.PushText(Loc.GetString("genpop-prisoner-id-examine-wait",
                    ("minutes", served.Minutes),
                    ("seconds", served.Seconds),
                    ("sentence", sentence.TotalMinutes),
                    ("crime", ent.Comp.Crime)));
            }
        }
    }

    protected virtual void CreateId(Entity<GenpopLockerComponent> ent, string name, float sentence, string crime)
    {

    }
}
