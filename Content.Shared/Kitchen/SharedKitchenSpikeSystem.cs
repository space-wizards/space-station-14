using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
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
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KitchenSpikeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<KitchenSpikeComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<KitchenSpikeComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<KitchenSpikeComponent, CanDropTargetEvent>(OnCanDrop);
        SubscribeLocalEvent<KitchenSpikeComponent, DragDropTargetEvent>(OnDragDrop);
        SubscribeLocalEvent<KitchenSpikeComponent, SuicideByEnvironmentEvent>(OnSuicideByEnvironment);
        SubscribeLocalEvent<KitchenSpikeComponent, SpikeDoAfterEvent>(OnDoAfter);
    }

    private void OnInit(Entity<KitchenSpikeComponent> ent, ref ComponentInit args)
    {
        ent.Comp.BodyContainer =  _containerSystem.EnsureContainer<ContainerSlot>(ent, ent.Comp.ContainerId);
    }

    private void OnInteractHand(Entity<KitchenSpikeComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.BodyContainer.ContainedEntity.HasValue)
            _popupSystem.PopupEntity(Loc.GetString("comp-kitchen-spike-knife-needed"), ent, args.User);

        args.Handled = true;
    }

    private void OnInteractUsing(Entity<KitchenSpikeComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<SharpComponent>(args.Used))
        {
            _popupSystem.PopupPredicted(Loc.GetString("comp-kitchen-spike-knife-needed"), ent, args.User);
            return;
        }

        var victim = ent.Comp.BodyContainer.ContainedEntity;

        if (!TryComp<ButcherableComponent>(victim, out var butcherable))
            return;

        var uid = Spawn(_random.PickAndTake(butcherable.SpawnedEntities).PrototypeId, Transform(ent).Coordinates);
        _metaDataSystem.SetEntityName(ent, Loc.GetString("comp-kitchen-spike-meat-name", ("name", Name(uid)), ("victim", victim)));

        args.Handled = true;
    }

    private void OnCanDrop(Entity<KitchenSpikeComponent> ent, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.CanDrop = CanSpike(ent, args.Dragged);
        args.Handled = true;
    }

    private void OnDragDrop(Entity<KitchenSpikeComponent> ent, ref DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ButcherableComponent>(args.Dragged, out var butcherable))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay + butcherable.ButcherDelay, new SpikeDoAfterEvent(), ent, target: args.Dragged, used: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };

        _doAfterSystem.TryStartDoAfter(doAfterArgs);

        args.Handled = true;
    }

    private void OnSuicideByEnvironment(Entity<KitchenSpikeComponent> ent, ref SuicideByEnvironmentEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TrySpike(ent, args.Victim, args.Victim);
    }

    private void OnDoAfter(Entity<KitchenSpikeComponent> ent, ref SpikeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !args.Target.HasValue)
            return;

        TrySpike(ent, args.Target.Value, args.User);

        args.Handled = true;
    }

    /// <summary>
    ///
    /// </summary>
    private bool CanSpike(Entity<KitchenSpikeComponent> ent, Entity<ButcherableComponent?> butcheredEnt)
    {
        if (!Resolve(butcheredEnt.Owner, ref butcheredEnt.Comp, false))
            return false;

        if (butcheredEnt.Comp.Type != ButcheringType.Spike)
            return false;

        if (!_containerSystem.CanInsert(butcheredEnt, ent.Comp.BodyContainer))
            return false;

        return true;
    }

    /// <summary>
    ///
    /// </summary>
    private bool TrySpike(Entity<KitchenSpikeComponent> ent, Entity<ButcherableComponent?> butcheredEnt, EntityUid user)
    {
        if (!CanSpike(ent, butcheredEnt))
            return false;

        if (!_containerSystem.Insert(butcheredEnt.Owner, ent.Comp.BodyContainer))
            return false;



        return true;
    }
}

/// <summary>
///
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SpikeDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
///
/// </summary>
public sealed class SpikeAttemptEvent : CancellableEntityEventArgs;
