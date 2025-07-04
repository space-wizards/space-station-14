using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Kitchen.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Kitchen;

/// <summary>
///
/// </summary>
public sealed class SharedKitchenSpikeSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly HashSet<EntityUid> _activeSpikes = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KitchenSpikeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<KitchenSpikeComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<KitchenSpikeComponent, EntInsertedIntoContainerMessage>(OnEntInsertedIntoContainer);
        SubscribeLocalEvent<KitchenSpikeComponent, EntRemovedFromContainerMessage>(OnEntRemovedFromContainer);
        SubscribeLocalEvent<KitchenSpikeComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<KitchenSpikeComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<KitchenSpikeComponent, CanDropTargetEvent>(OnCanDrop);
        SubscribeLocalEvent<KitchenSpikeComponent, DragDropTargetEvent>(OnDragDrop);
        SubscribeLocalEvent<KitchenSpikeComponent, SpikeDoAfterEvent>(OnSpikeDoAfter);
        SubscribeLocalEvent<KitchenSpikeComponent, SpikeButcherDoAfterEvent>(OnSpikeButcherDoAfter);
        SubscribeLocalEvent<KitchenSpikeComponent, ExaminedEvent>(OnSpikeExamined);

        SubscribeLocalEvent<KitchenSpikeVictimComponent, ExaminedEvent>(OnVictimExamined);
    }

    private void OnInit(Entity<KitchenSpikeComponent> ent, ref ComponentInit args)
    {
        ent.Comp.BodyContainer =  _containerSystem.EnsureContainer<ContainerSlot>(ent, ent.Comp.ContainerId);
    }

    private void OnInsertAttempt(Entity<KitchenSpikeComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled || TryComp<ButcherableComponent>(args.EntityUid, out var butcherable) &&
            butcherable.Type == ButcheringType.Spike)
            return;

        args.Cancel();
    }

    private void OnEntInsertedIntoContainer(Entity<KitchenSpikeComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        _damageableSystem.TryChangeDamage(args.Entity, ent.Comp.SpikeDamage, true);
        _activeSpikes.Add(ent);
    }

    private void OnEntRemovedFromContainer(Entity<KitchenSpikeComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        _damageableSystem.TryChangeDamage(args.Entity, ent.Comp.SpikeDamage, true);
        _activeSpikes.Remove(ent);
    }

    private void OnInteractHand(Entity<KitchenSpikeComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        var victim = ent.Comp.BodyContainer.ContainedEntity;

        if (!victim.HasValue)
            return;

        _popupSystem.PopupClient(Loc.GetString("butcherable-need-knife",
            ("target", Identity.Entity(victim.Value, EntityManager))),
            ent,
            args.User,
            PopupType.Medium);

        args.Handled = true;
    }

    private void OnInteractUsing(Entity<KitchenSpikeComponent> ent, ref InteractUsingEvent args)
    {
        var victim = ent.Comp.BodyContainer.ContainedEntity;

        if (args.Handled || !TryComp<ButcherableComponent>(victim, out var butcherable) || butcherable.SpawnedEntities.Count == 0 ||
            !TryComp<MobStateComponent>(victim, out var mobState))
            return;

        if (!HasComp<SharpComponent>(args.Used))
        {
            _popupSystem.PopupClient(Loc.GetString("butcherable-need-knife",
                    ("target", Identity.Entity(victim.Value, EntityManager))),
                    ent,
                    args.User,
                    PopupType.Medium);

            args.Handled = true;
            return;
        }

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            mobState.CurrentState == MobState.Dead ? butcherable.ButcherDelay : butcherable.ButcherDelay + ent.Comp.DelayAlive,
            new SpikeButcherDoAfterEvent(),
            ent,
            target: victim,
            used: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        });

        args.Handled = true;
    }

    private void OnCanDrop(Entity<KitchenSpikeComponent> ent, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.CanDrop = _containerSystem.CanInsert(args.Dragged, ent.Comp.BodyContainer);
        args.Handled = true;
    }

    private void OnDragDrop(Entity<KitchenSpikeComponent> ent, ref DragDropTargetEvent args)
    {
        if (args.Handled || !TryComp<ButcherableComponent>(args.Dragged, out var butcherable))
            return;

        ShowPopups("comp-kitchen-spike-begin-hook-self",
            "comp-kitchen-spike-begin-hook-self-other",
            "comp-kitchen-spike-begin-hook-other-self",
            "comp-kitchen-spike-begin-hook-other",
            args.User,
            args.Dragged,
            ent);

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.Delay + butcherable.ButcherDelay,
            new SpikeDoAfterEvent(),
            ent,
            target: args.Dragged,
            used: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        });

        args.Handled = true;
    }

    private void OnSpikeDoAfter(Entity<KitchenSpikeComponent> ent, ref SpikeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !args.Target.HasValue)
            return;

        if (_containerSystem.Insert(args.Target.Value, ent.Comp.BodyContainer))
        {
            ShowPopups("comp-kitchen-spike-hook-self",
                "comp-kitchen-spike-hook-self-other",
                "comp-kitchen-spike-hook-other-self",
                "comp-kitchen-spike-hook-other",
                args.User,
                args.Target.Value,
                ent);
        }

        args.Handled = true;
    }

    private void OnSpikeButcherDoAfter(Entity<KitchenSpikeComponent> ent, ref SpikeButcherDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !args.Target.HasValue || !args.Used.HasValue)
            return;

        if (!TryComp<ButcherableComponent>(args.Target, out var butcherable) || butcherable.SpawnedEntities.Count == 0)
            return;

        var index = _random.Next(butcherable.SpawnedEntities.Count);
        var entry = butcherable.SpawnedEntities[index];
        entry.Amount--;

        var uid = PredictedSpawnNextToOrDrop(entry.PrototypeId, ent);
        _metaDataSystem.SetEntityName(uid,
            Loc.GetString("comp-kitchen-spike-meat-name",
                ("name", Name(uid)),
                ("victim", args.Target)));

        if (entry.Amount <= 0)
            butcherable.SpawnedEntities.RemoveAt(index);
        else
            butcherable.SpawnedEntities[index] = entry;

        if (butcherable.SpawnedEntities.Count == 0)
            _bodySystem.GibBody(args.Target.Value, true);
        else
        {
            Dirty(args.Target.Value, butcherable);
            _damageableSystem.TryChangeDamage(args.Target, ent.Comp.ButcherDamage, true);
        }

        _popupSystem.PopupClient(Loc.GetString("butcherable-knife-butchered-success",
            ("target", Identity.Entity(args.Target.Value, EntityManager)),
            ("knife", args.Used.Value)),
            ent,
            args.User,
            PopupType.Medium);

        args.Handled = true;
    }

    private void OnSpikeExamined(Entity<KitchenSpikeComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.BodyContainer.ContainedEntity.HasValue)
            return;

        args.PushMarkup(
            Loc.GetString("comp-kitchen-spike-hooked",
                ("victim", Identity.Entity(ent.Comp.BodyContainer.ContainedEntity.Value, EntityManager))),
            1);
        args.PushMessage(_examineSystem.GetExamineText(ent.Comp.BodyContainer.ContainedEntity.Value, args.Examiner));
    }

    private void OnVictimExamined(Entity<KitchenSpikeVictimComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("comp-kitchen-spike-victim-examine", ("target", Identity.Entity(ent, EntityManager))));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var uid in _activeSpikes)
        {
            if (!TryComp<KitchenSpikeComponent>(uid, out var spike))
            {
                _activeSpikes.Remove(uid);
                continue;
            }

            if (spike.NextUpdate > _gameTiming.CurTime)
                continue;

            spike.NextUpdate += spike.UpdateInterval;
            Dirty(uid, spike);

            _damageableSystem.TryChangeDamage(spike.BodyContainer.ContainedEntity, spike.UpdateDamage, true);
        }
    }

    /// <summary>
    ///
    /// </summary>
    private void ShowPopups(string selfLocMessageSelf,
        string selfLocMessageOthers,
        string locMessageSelf,
        string locMessageOthers,
        EntityUid user,
        EntityUid victim,
        EntityUid hook)
    {
        string messageSelf, messageOthers;

        var victimIdentity = Identity.Entity(victim, EntityManager);

        if (user == victim)
        {
            messageSelf = Loc.GetString(selfLocMessageSelf, ("hook", hook));
            messageOthers = Loc.GetString(selfLocMessageOthers, ("victim", victimIdentity), ("hook", hook));
        }
        else
        {
            messageSelf = Loc.GetString(locMessageSelf, ("victim", victimIdentity), ("hook", hook));
            messageOthers = Loc.GetString(locMessageOthers,
                ("user", Identity.Entity(user, EntityManager)),
                ("victim", victimIdentity),
                ("hook", hook));
        }

        _popupSystem.PopupPredicted(messageSelf, messageOthers, hook, user, PopupType.MediumCaution);
    }
}

[Serializable, NetSerializable]
public sealed partial class SpikeDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class SpikeButcherDoAfterEvent : SimpleDoAfterEvent;
