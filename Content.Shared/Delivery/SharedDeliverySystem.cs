using System.Linq;
using Content.Shared.Shuttles.Components;
using Content.Shared.Examine;
using Content.Shared.FingerprintReader;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Objectives.Components;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Delivery;

/// <summary>
/// Shared side of the DeliverySystem.
/// This covers for letters/packages, as well as spawning a reward for the player upon opening.
/// </summary>
public abstract class SharedDeliverySystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly FingerprintReaderSystem _fingerprintReader = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;

    private static readonly ProtoId<TagPrototype> TrashTag = "Trash";
    private static readonly ProtoId<TagPrototype> RecyclableTag = "Recyclable";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeliveryComponent, ExaminedEvent>(OnDeliveryExamine);
        SubscribeLocalEvent<DeliveryComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<DeliveryComponent, GetVerbsEvent<AlternativeVerb>>(OnGetDeliveryVerbs);
        SubscribeLocalEvent<DeliveryComponent, AttemptSimpleToolUseEvent>(OnAttemptSimpleToolUse);
        SubscribeLocalEvent<DeliveryComponent, SimpleToolDoAfterEvent>(OnSimpleToolUse);

        SubscribeLocalEvent<DeliverySpawnerComponent, ExaminedEvent>(OnSpawnerExamine);
        SubscribeLocalEvent<DeliverySpawnerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetSpawnerVerbs);
    }

    private void OnDeliveryExamine(Entity<DeliveryComponent> ent, ref ExaminedEvent args)
    {
        var jobTitle = ent.Comp.RecipientJobTitle ?? Loc.GetString("delivery-recipient-no-job");
        var recipientName = ent.Comp.RecipientName ?? Loc.GetString("delivery-recipient-no-name");

        using (args.PushGroup(nameof(DeliveryComponent), 1))
        {
            if (ent.Comp.IsOpened)
            {
                args.PushText(Loc.GetString("delivery-already-opened-examine"));
            }

            args.PushText(Loc.GetString("delivery-recipient-examine", ("recipient", recipientName), ("job", jobTitle)));
        }

        if (ent.Comp.IsLocked)
        {
            var multiplier = GetDeliveryMultiplier(ent);
            var totalSpesos = Math.Round(ent.Comp.BaseSpesoReward * multiplier);

            args.PushMarkup(Loc.GetString("delivery-earnings-examine", ("spesos", totalSpesos)), -1);
        }
    }

    private void OnSpawnerExamine(Entity<DeliverySpawnerComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("delivery-teleporter-amount-examine", ("amount", ent.Comp.ContainedDeliveryAmount)), 50);
    }

    private void OnUseInHand(Entity<DeliveryComponent> ent, ref UseInHandEvent args)
    {
        args.Handled = true;

        if (ent.Comp.IsOpened)
            return;

        if (ent.Comp.IsLocked)
            TryUnlockDelivery(ent, args.User);
        else
            OpenDelivery(ent, args.User);
    }

    private void OnGetDeliveryVerbs(Entity<DeliveryComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || ent.Comp.IsOpened)
            return;

        if (_hands.IsHolding(args.User, ent))
            return;

        var user = args.User;

        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () =>
            {
                if (ent.Comp.IsLocked)
                    TryUnlockDelivery(ent, user);
                else
                    OpenDelivery(ent, user, false);
            },
            Text = ent.Comp.IsLocked ? Loc.GetString("delivery-unlock-verb") : Loc.GetString("delivery-open-verb"),
        });
    }


    private void OnAttemptSimpleToolUse(Entity<DeliveryComponent> ent, ref AttemptSimpleToolUseEvent args)
    {
        if (ent.Comp.IsOpened || !ent.Comp.IsLocked)
            args.Cancelled = true;
    }

    private void OnSimpleToolUse(Entity<DeliveryComponent> ent, ref SimpleToolDoAfterEvent args)
    {
        if (ent.Comp.IsOpened || args.Cancelled)
            return;

        HandlePenalty(ent);

        TryUnlockDelivery(ent, args.User, false, true);
        OpenDelivery(ent, args.User, false, true);
    }

    private void OnGetSpawnerVerbs(Entity<DeliverySpawnerComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var user = args.User;

        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () =>
            {
                _audio.PlayPredicted(ent.Comp.OpenSound, ent.Owner, user);

                if(ent.Comp.ContainedDeliveryAmount == 0)
                {
                    _popup.PopupPredicted(Loc.GetString("delivery-teleporter-empty", ("entity", ent)), null, ent, user);
                    return;
                }

                SpawnDeliveries(ent.Owner);

                UpdateDeliverySpawnerVisuals(ent, ent.Comp.ContainedDeliveryAmount);
            },
            Text = Loc.GetString("delivery-teleporter-empty-verb"),
        });
    }

    private bool TryUnlockDelivery(Entity<DeliveryComponent> ent, EntityUid user, bool rewardMoney = true, bool force = false)
    {
        // Check fingerprint access if there is a reader on the mail
        if (!force && !_fingerprintReader.IsAllowed(ent.Owner, user, out _))
            return false;

        var deliveryName = _nameModifier.GetBaseName(ent.Owner);

        if (!force)
            _audio.PlayPredicted(ent.Comp.UnlockSound, user, user);

        ent.Comp.IsLocked = false;
        UpdateAntiTamperVisuals(ent, ent.Comp.IsLocked);

        DirtyField(ent, ent.Comp, nameof(DeliveryComponent.IsLocked));

        RemCompDeferred<SimpleToolUsageComponent>(ent); // we don't want unlocked mail to still be cuttable

        var ev = new DeliveryUnlockedEvent(user);
        RaiseLocalEvent(ent, ref ev);

        if (rewardMoney)
            GrantSpesoReward(ent.AsNullable());

        if (!force)
            _popup.PopupPredicted(Loc.GetString("delivery-unlocked-self", ("delivery", deliveryName)),
                Loc.GetString("delivery-unlocked-others", ("delivery", deliveryName), ("recipient", Identity.Entity(user, EntityManager)), ("possadj", user)), user, user);

        return true;
    }

    private void OpenDelivery(Entity<DeliveryComponent> ent, EntityUid user, bool attemptPickup = true, bool force = false)
    {
        var deliveryName = _nameModifier.GetBaseName(ent.Owner);

        _audio.PlayPredicted(ent.Comp.OpenSound, user, user);

        var ev = new DeliveryOpenedEvent(user);
        RaiseLocalEvent(ent, ref ev);

        if (attemptPickup)
            _hands.TryDrop(user, ent);

        ent.Comp.IsOpened = true;
        _appearance.SetData(ent, DeliveryVisuals.IsTrash, ent.Comp.IsOpened);

        _tag.AddTags(ent, TrashTag, RecyclableTag);
        EnsureComp<SpaceGarbageComponent>(ent);
        RemCompDeferred<StealTargetComponent>(ent); // opened mail should not count for the objective

        DirtyField(ent.Owner, ent.Comp, nameof(DeliveryComponent.IsOpened));

        if (!force)
            _popup.PopupPredicted(Loc.GetString("delivery-opened-self", ("delivery", deliveryName)),
                Loc.GetString("delivery-opened-others", ("delivery", deliveryName), ("recipient", Identity.Entity(user, EntityManager)), ("possadj", user)), user, user);

        if (!_container.TryGetContainer(ent, ent.Comp.Container, out var container))
            return;

        if (attemptPickup)
        {
            foreach (var entity in container.ContainedEntities.ToArray())
            {
                _hands.PickupOrDrop(user, entity);
            }
        }
        else
        {
            _container.EmptyContainer(container, true);
        }
    }

    #region Visual Updates
    // TODO: generic updateVisuals from component data
    private void UpdateAntiTamperVisuals(EntityUid uid, bool isLocked)
    {
        _appearance.SetData(uid, DeliveryVisuals.IsLocked, isLocked);

        // If we're trying to unlock, mark priority as inactive
        if (HasComp<DeliveryPriorityComponent>(uid))
            _appearance.SetData(uid, DeliveryVisuals.PriorityState, DeliveryPriorityState.Inactive);
    }

    public void UpdatePriorityVisuals(Entity<DeliveryPriorityComponent> ent)
    {
        if (!TryComp<DeliveryComponent>(ent, out var delivery))
            return;

        if (delivery.IsLocked && !delivery.IsOpened)
        {
            _appearance.SetData(ent, DeliveryVisuals.PriorityState, ent.Comp.Expired ? DeliveryPriorityState.Inactive : DeliveryPriorityState.Active);
        }
    }

    public void UpdateBrokenVisuals(Entity<DeliveryFragileComponent> ent, bool isFragile)
    {
        _appearance.SetData(ent, DeliveryVisuals.IsBroken, ent.Comp.Broken);
        _appearance.SetData(ent, DeliveryVisuals.IsFragile, isFragile);
    }

    public void UpdateBombVisuals(Entity<DeliveryBombComponent> ent)
    {
        var isPrimed = HasComp<PrimedDeliveryBombComponent>(ent);

        _appearance.SetData(ent, DeliveryVisuals.IsBomb, isPrimed ? DeliveryBombState.Primed : DeliveryBombState.Inactive);
    }

    protected void UpdateDeliverySpawnerVisuals(EntityUid uid, int contents)
    {
        _appearance.SetData(uid, DeliverySpawnerVisuals.Contents, contents > 0);
    }
    #endregion

    /// <summary>
    /// Gathers the total multiplier for a delivery.
    /// This is done by components having subscribed to GetDeliveryMultiplierEvent and having added onto it.
    /// </summary>
    /// <param name="ent">The delivery for which to get the multiplier.</param>
    /// <returns>Total multiplier.</returns>
    protected float GetDeliveryMultiplier(Entity<DeliveryComponent> ent)
    {
        var ev = new GetDeliveryMultiplierEvent();
        RaiseLocalEvent(ent, ref ev);

        // Ensure the multiplier can never go below 0.
        var totalMultiplier = Math.Max(ev.AdditiveMultiplier * ev.MultiplicativeMultiplier, 0);

        return totalMultiplier;
    }

    protected virtual void GrantSpesoReward(Entity<DeliveryComponent?> ent) { }

    protected virtual void HandlePenalty(Entity<DeliveryComponent> ent, string? reason = null) { }

    protected virtual void SpawnDeliveries(Entity<DeliverySpawnerComponent?> ent) { }
}

/// <summary>
/// Used to gather the total multiplier for deliveries.
/// This is done by various modifier components subscribing to this and adding accordingly.
/// </summary>
/// <param name="AdditiveMultiplier">The additive multiplier.</param>
/// <param name="MultiplicativeMultiplier">The multiplicative multiplier.</param>
[ByRefEvent]
public record struct GetDeliveryMultiplierEvent(float AdditiveMultiplier, float MultiplicativeMultiplier)
{
    // we can't use an optional parameter because the default parameterless constructor defaults everything
    public GetDeliveryMultiplierEvent() : this(1.0f, 1.0f) { }
}

/// <summary>
/// Event raised on the delivery when it is unlocked.
/// </summary>
[ByRefEvent]
public readonly record struct DeliveryUnlockedEvent(EntityUid User);

/// <summary>
/// Event raised on the delivery when it is opened.
/// </summary>
[ByRefEvent]
public readonly record struct DeliveryOpenedEvent(EntityUid User);
