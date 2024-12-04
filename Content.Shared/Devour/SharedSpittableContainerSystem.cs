using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.SpittableContainer.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.SpittableContainer;

public abstract class SharedSpittableContainerSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] protected readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpittableContainerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SpittableContainerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SpittableContainerComponent, SwallowToContainerActionEvent>(OnSwallowToContainerAction);
        SubscribeLocalEvent<SpittableContainerComponent, SwallowDoAfterEvent>(OnSwallowDoAfter);
        SubscribeLocalEvent<SpittableContainerComponent, SpitFromContainerActionEvent>(OnSpitFromContainerAction);
        SubscribeLocalEvent<SpittableContainerComponent, SpitFromContainerDoAfterEvent>(OnSpitDoAfter);
    }

    private void OnInit(Entity<SpittableContainerComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Container = _containerSystem.EnsureContainer<Container>(ent.Owner, ent.Comp.Storage);

        EntityUid? SwallowActionUid = null;
        _actionsSystem.AddAction(ent.Owner, ref SwallowActionUid, ent.Comp.SwallowActionPrototype);
        if (SwallowActionUid != null)
            ent.Comp.SwallowActionEntity = SwallowActionUid.Value;

        EntityUid? SpitActionUid = null;
        _actionsSystem.AddAction(ent.Owner, ref SpitActionUid, ent.Comp.SpitContainerActionPrototype);
        if (SpitActionUid != null)
            ent.Comp.SwallowActionEntity = SpitActionUid.Value;
    }

    private void OnShutdown(Entity<SpittableContainerComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.SwallowActionEntity);
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.SpitContainerActionEntity);
    }

    private void OnSwallowToContainerAction(Entity<SpittableContainerComponent> ent, ref SwallowToContainerActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var target = args.Target;

        if (!HasComp<ItemComponent>(target))
            return;

        if (!_containerSystem.CanInsert(target, ent.Comp.Container))
            return;

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, ent.Owner, ent.Comp.SwallowTime, new SwallowDoAfterEvent(), ent.Owner, target: target, used: ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        });
    }

    private void OnSwallowDoAfter(Entity<SpittableContainerComponent> ent, ref SwallowDoAfterEvent args)
    {
        if (args.Target == null || args.Cancelled)
            return;

        if (!_containerSystem.CanInsert(args.Target.Value, ent.Comp.Container))
            return;

        _popupSystem.PopupPredicted(Loc.GetString("disease-vomit", ("person", Identity.Entity(ent.Owner, EntityManager))), ent.Owner, ent.Owner);
        _audioSystem.PlayPredicted(ent.Comp.SoundEat, ent.Owner, ent.Owner, ent.Comp.SoundEat.Params);
        _containerSystem.InsertOrDrop(args.Target.Value, ent.Comp.Container);
    }

    private void OnSpitFromContainerAction(Entity<SpittableContainerComponent> ent, ref SpitFromContainerActionEvent args)
    {
        if (args.Handled || ent.Comp.Container.Count == 0)
            return;

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, ent.Owner, ent.Comp.SpitTime, new SpitFromContainerDoAfterEvent(), ent.Owner));
    }

    private void OnSpitDoAfter(Entity<SpittableContainerComponent> ent, ref SpitFromContainerDoAfterEvent args)
    {
        if (ent.Comp.Container.Count == 0 || args.Cancelled)
            return;

        _popupSystem.PopupPredicted(Loc.GetString("disease-vomit", ("person", Identity.Entity(ent.Owner, EntityManager))), ent.Owner, ent.Owner);
        _audioSystem.PlayPredicted(ent.Comp.SoundSpit, ent.Owner, ent.Owner, ent.Comp.SoundSpit.Params);
        _containerSystem.EmptyContainer(ent.Comp.Container);
    }
}

public sealed partial class SwallowToContainerActionEvent : EntityTargetActionEvent;

public sealed partial class SpitFromContainerActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class SwallowDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class SpitFromContainerDoAfterEvent : SimpleDoAfterEvent;
