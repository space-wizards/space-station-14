using Content.Shared.Actions;
using Content.Shared.Body.Events;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.SpittableContainer.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.SpittableContainer;

/// <summary>
/// Manages SpittableContainerComponent.
/// Allows entities to swallow and spit items using a provided container with a granted action.
/// </summary>
public sealed class SpittableContainerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpittableContainerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SpittableContainerComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<SpittableContainerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SpittableContainerComponent, SwallowToContainerActionEvent>(OnSwallowToContainerAction);
        SubscribeLocalEvent<SpittableContainerComponent, SwallowDoAfterEvent>(OnSwallowDoAfter);
        SubscribeLocalEvent<SpittableContainerComponent, SpitFromContainerActionEvent>(OnSpitFromContainerAction);
        SubscribeLocalEvent<SpittableContainerComponent, SpitFromContainerDoAfterEvent>(OnSpitDoAfter);
        SubscribeLocalEvent<SpittableContainerComponent, BeingGibbedEvent>(OnGibbed);
    }

    private void OnCompInit(Entity<SpittableContainerComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Container = _containerSystem.EnsureContainer<Container>(ent, ent.Comp.Storage);
    }

    private void OnMapInit(Entity<SpittableContainerComponent> ent, ref MapInitEvent args)
    {
        _actionsSystem.AddAction(ent, ref ent.Comp.SwallowActionEntity, ent.Comp.SwallowActionPrototype);
        _actionsSystem.AddAction(ent, ref ent.Comp.SpitContainerActionEntity, ent.Comp.SpitContainerActionPrototype);
    }

    private void OnShutdown(Entity<SpittableContainerComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.SwallowActionEntity);
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.SpitContainerActionEntity);
    }

    private void OnGibbed(Entity<SpittableContainerComponent> ent, ref BeingGibbedEvent args)
    {
        if (ent.Comp.Container != null)
            _containerSystem.EmptyContainer(ent.Comp.Container);
    }

    private void OnSwallowToContainerAction(Entity<SpittableContainerComponent> ent, ref SwallowToContainerActionEvent args)
    {
        if (args.Handled
            || !HasComp<ItemComponent>(args.Target))
            return;

        if (ent.Comp.Container == null)
            return;

        if (!_containerSystem.CanInsert(args.Target, ent.Comp.Container))
        {
            _popupSystem.PopupClient(Loc.GetString(ent.Comp.SwallowFailPopup), ent, ent);
            return;
        }

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
            ent,
            ent.Comp.SwallowTime,
            new SwallowDoAfterEvent(),
            ent,
            target: args.Target,
            used: ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        });

        args.Handled = true;
    }

    private void OnSwallowDoAfter(Entity<SpittableContainerComponent> ent, ref SwallowDoAfterEvent args)
    {
        if (args.Handled
            || args.Target == null
            || args.Cancelled
            || ent.Comp.Container == null
            || !_containerSystem.CanInsert(args.Target.Value, ent.Comp.Container))
            return;

        if (ent.Comp.SoundEat != null)
            _audioSystem.PlayPredicted(ent.Comp.SoundEat, ent, ent, ent.Comp.SoundEat.Params);

        _containerSystem.InsertOrDrop(args.Target.Value, ent.Comp.Container);

        args.Handled = true;
    }

    private void OnSpitFromContainerAction(Entity<SpittableContainerComponent> ent, ref SpitFromContainerActionEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.Container == null)
            return;

        if (ent.Comp.Container.Count == 0)
        {
            _popupSystem.PopupClient(Loc.GetString("spittable-container-spit-empty"),
                ent,
                ent);
            return;
        }

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, ent, ent.Comp.SpitTime, new SpitFromContainerDoAfterEvent(), ent));

        args.Handled = true;
    }

    private void OnSpitDoAfter(Entity<SpittableContainerComponent> ent, ref SpitFromContainerDoAfterEvent args)
    {
        if (args.Handled || ent.Comp.Container == null || ent.Comp.Container.Count == 0 || args.Cancelled)
            return;

        if (ent.Comp.SpitPopup != null)
            _popupSystem.PopupPredicted(Loc.GetString(ent.Comp.SpitPopup, ("person", Identity.Entity(ent.Owner, EntityManager))), ent, ent);

        if (ent.Comp.SoundSpit != null)
            _audioSystem.PlayPredicted(ent.Comp.SoundSpit, ent.Owner, ent.Owner);

        _containerSystem.EmptyContainer(ent.Comp.Container);

        args.Handled = true;
    }
}

/// <summary>
/// Raised on the user when using the Store action provided by SpittableContainerComponent.
/// </summary>
public sealed partial class SwallowToContainerActionEvent : EntityTargetActionEvent;

/// <summary>
/// Raised on the user when using the Spit action provided by SpittableContainerComponent.
/// </summary>
public sealed partial class SpitFromContainerActionEvent : InstantActionEvent;

/// <summary>
/// DoAfter that manages swallowing an item with the Store action.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SwallowDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// DoAfter that manages spitting out an item with the Spit action.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SpitFromContainerDoAfterEvent : SimpleDoAfterEvent;
