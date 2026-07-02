using System.Diagnostics.CodeAnalysis;
using Content.Shared.DoAfter;
using Content.Shared.Fluids;
using Content.Shared.Forensics.Components;
using Content.Shared.Forensics.Events;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.Forensics.Systems;

public abstract partial class SharedForensicsSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;

    [Dependency] private EntityQuery<ForensicsComponent> _forensicsQuery = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CleansForensicsComponent, AfterInteractEvent>(OnAfterInteract, before: [typeof(IngestionSystem)], after: [typeof(SharedAbsorbentSystem)]);
        SubscribeLocalEvent<CleansForensicsComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);

        SubscribeLocalEvent<FingerprintComponent, TryAccessFingerprintEvent>(OnFingerprintAccessAttempt);
    }

    private void OnAfterInteract(Entity<CleansForensicsComponent> cleanForensicsEntity, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        args.Handled = TryStartCleaning(cleanForensicsEntity, args.User, args.Target.Value);
    }

    private void OnUtilityVerb(Entity<CleansForensicsComponent> entity, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        // These need to be set outside for the anonymous method!
        var user = args.User;
        var target = args.Target;
        // Whether it's actually cleanable.
        var canBeCleaned = _forensicsQuery.TryComp(args.Target, out var comp) && comp.IsDirty;
        var message = canBeCleaned
            ? Loc.GetString("forensics-verb-message")
            : Loc.GetString("forensics-cleaning-cannot-clean", ("target", target));

        var verb = new UtilityVerb
        {
            Act = () => TryStartCleaning(entity, user, target),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/bubbles.svg.192dpi.png")),
            Text = Loc.GetString("forensics-verb-text"),
            Disabled = !canBeCleaned,
            Message = message,
            // This is important because if its true using the cleaning device will count as touching the object.
            DoContactInteraction = false,
        };

        args.Verbs.Add(verb);
    }

    /// <summary>
    /// Attempts to clean the given item with the given CleansForensics entity.
    /// </summary>
    /// <param name="cleanForensicsEntity">The entity that is being used to clean the target.</param>
    /// <param name="user">The user that is using the cleanForensicsEntity.</param>
    /// <param name="target">The target of the forensics clean.</param>
    /// <returns>True if the target can be cleaned and has some sort of DNA or fingerprints / fibers and false otherwise.</returns>
    public bool TryStartCleaning(Entity<CleansForensicsComponent> cleanForensicsEntity, EntityUid user, EntityUid target)
    {
        if (!_forensicsQuery.TryComp(target, out var forensicsComp))
        {
            _popupSystem.PopupClient(Loc.GetString("forensics-cleaning-cannot-clean", ("target", Identity.Entity(target, EntityManager))), user, user, PopupType.MediumCaution);
            return false;
        }

        if (forensicsComp.IsDirty)
        {
            var cleanDelay = cleanForensicsEntity.Comp.CleanDelay;
            var doAfterArgs = new DoAfterArgs(EntityManager, user, cleanDelay, new CleanForensicsDoAfterEvent(), cleanForensicsEntity, target: target, used: cleanForensicsEntity)
            {
                NeedHand = true,
                BreakOnDamage = true,
                BreakOnMove = true,
                MovementThreshold = 0.01f,
                DistanceThreshold = forensicsComp.CleanDistance,
            };

            _doAfterSystem.TryStartDoAfter(doAfterArgs);

            var userPopupText = Loc.GetString("forensics-cleaning-user", ("target", Identity.Entity(target, EntityManager)));
            var othersPopupText = Loc.GetString("forensics-cleaning-others", ("user", Identity.Entity(user, EntityManager)), ("target", Identity.Entity(target, EntityManager)));
            _popupSystem.PopupPredicted(userPopupText, othersPopupText, user, user);

            return true;
        }

        _popupSystem.PopupClient(Loc.GetString("forensics-cleaning-cannot-clean", ("target", Identity.Entity(target, EntityManager))), user, user, PopupType.MediumCaution);
        return false;
    }

    private void OnFingerprintAccessAttempt(Entity<FingerprintComponent> ent, ref TryAccessFingerprintEvent args)
    {
        args.Fingerprint = ent.Comp.Fingerprint;
    }

    #region Public API

    public virtual void RandomizeDNA(Entity<DnaComponent?> dnaOwner) {}

    public virtual void RandomizeFingerprint(Entity<FingerprintComponent?> fingerprintOwner) {}

    public virtual void TransferDna(EntityUid recipient, EntityUid donor, bool canDnaBeCleaned = true) {}

    /// <summary>
    /// Checks if there's a way to access the fingerprint of the target entity.
    /// </summary>
    /// <param name="target">The entity with the fingerprint</param>
    /// <param name="blocker">The entity that blocked accessing the fingerprint</param>
    /// <param name="fingerprint">The fingerprints of the entity.</param>
    /// <returns>True if the target has both a fingerprint and nothing blocking it, false if otherwise.</returns>
    public bool TryAccessFingerprint(EntityUid target, out EntityUid? blocker, [NotNullWhen(true)] out string? fingerprint)
    {
        var ev = new TryAccessFingerprintEvent();
        RaiseLocalEvent(target, ref ev);

        fingerprint = ev.Fingerprint;
        blocker = ev.Blocker;
        return !ev.Blocker.HasValue && ev.Fingerprint != null;
    }
    #endregion

}
