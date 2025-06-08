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

        if (ent.Comp.IsOpened)
        {
            args.PushText(Loc.GetString("delivery-already-opened-examine"));
        }

        args.PushText(Loc.GetString("delivery-recipient-examine", ("recipient", recipientName), ("job", jobTitle)));
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
        if (!force && TryComp<FingerprintReaderComponent>(ent, out var reader) && !_fingerprintReader.IsAllowed((ent, reader), user))
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
                Loc.GetString("delivery-unlocked-others", ("delivery", deliveryName), ("recipient", Identity.Name(user, EntityManager)), ("possadj", user)), user, user);

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
                Loc.GetString("delivery-opened-others", ("delivery", deliveryName), ("recipient", Identity.Name(user, EntityManager)), ("possadj", user)), user, user);

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

    // TODO: generic updateVisuals from component data
    private void UpdateAntiTamperVisuals(EntityUid uid, bool isLocked)
    {
        _appearance.SetData(uid, DeliveryVisuals.IsLocked, isLocked);

        // If we're trying to unlock, always remove the priority tape
        if (!isLocked)
            _appearance.SetData(uid, DeliveryVisuals.IsPriority, false);
    }

    protected void UpdateDeliverySpawnerVisuals(EntityUid uid, int contents)
    {
        _appearance.SetData(uid, DeliverySpawnerVisuals.Contents, contents > 0);
    }

    protected virtual void GrantSpesoReward(Entity<DeliveryComponent?> ent) { }

    protected virtual void HandlePenalty(Entity<DeliveryComponent> ent, string? reason = null) { }

    protected virtual void SpawnDeliveries(Entity<DeliverySpawnerComponent?> ent) { }
}

/// <summary>
/// Used to gather the multiplier from all different delivery components.
/// </summary>
[ByRefEvent]
public record struct GetDeliveryMultiplierEvent(float Multiplier)
{
    // we can't use an optional parameter because the default parameterless constructor defaults everything
    public GetDeliveryMultiplierEvent() : this(1.0f) { }
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
