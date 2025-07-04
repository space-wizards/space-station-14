using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Kitchen.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Kitchen;

/// <summary>
///
/// </summary>
public sealed class SharedKitchenSpikeSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly HashSet<EntityUid> _activeSpikes = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KitchenSpikeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<KitchenSpikeComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<KitchenSpikeComponent, EntGotRemovedFromContainerMessage>(OnEntRemovedIntoContainer);
        SubscribeLocalEvent<KitchenSpikeComponent, EntInsertedIntoContainerMessage>(OnEntInsertedIntoContainer);
        SubscribeLocalEvent<KitchenSpikeComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<KitchenSpikeComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<KitchenSpikeComponent, CanDropTargetEvent>(OnCanDrop);
        SubscribeLocalEvent<KitchenSpikeComponent, DragDropTargetEvent>(OnDragDrop);
        SubscribeLocalEvent<KitchenSpikeComponent, SpikeDoAfterEvent>(OnDoAfter);
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

    private void OnEntRemovedIntoContainer(Entity<KitchenSpikeComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        _damageableSystem.TryChangeDamage(args.Entity, ent.Comp.SpikeDamage, true);
        _activeSpikes.Remove(ent);
    }

    private void OnInteractHand(Entity<KitchenSpikeComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!ent.Comp.BodyContainer.ContainedEntity.HasValue)
            return;

        _popupSystem.PopupPredicted(Loc.GetString("comp-kitchen-spike-knife-needed"), ent, args.User);

        args.Handled = true;
    }

    private void OnInteractUsing(Entity<KitchenSpikeComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !TryComp<ButcherableComponent>(ent.Comp.BodyContainer.ContainedEntity, out var butcherable))
            return;

        if (!HasComp<SharpComponent>(args.Used))
        {
            _popupSystem.PopupPredicted(Loc.GetString("comp-kitchen-spike-knife-needed"), ent, args.User);
            return;
        }

        var uid = PredictedSpawnNextToOrDrop(_random.Pick(butcherable.SpawnedEntities).PrototypeId, ent);
        _metaDataSystem.SetEntityName(ent,
            Loc.GetString("comp-kitchen-spike-meat-name",
            ("name", Name(uid)),
            ("victim", ent.Comp.BodyContainer.ContainedEntity)));

        _damageableSystem.TryChangeDamage(args.Target, ent.Comp.ButcherDamage, true);

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

    private void OnDoAfter(Entity<KitchenSpikeComponent> ent, ref SpikeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !args.Target.HasValue)
            return;

        _containerSystem.Insert(args.Target.Value, ent.Comp.BodyContainer);

        args.Handled = true;
    }
}

[Serializable, NetSerializable]
public sealed partial class SpikeDoAfterEvent : SimpleDoAfterEvent;
