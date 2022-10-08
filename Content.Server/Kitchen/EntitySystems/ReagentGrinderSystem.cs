using System.Linq;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Kitchen.Components;
using Content.Server.Kitchen.Events;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Stack;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
using JetBrains.Annotations;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Kitchen.EntitySystems
{
    [UsedImplicitly]
    internal sealed class ReagentGrinderSystem : SharedReagentGrinderSystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly ContainerSystem _container = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionsSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _ui = default!;

        private Queue<ReagentGrinderComponent> _uiUpdateQueue = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ReagentGrinderComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<ReagentGrinderComponent, ComponentRemove>(OnComponentRemove);

            SubscribeLocalEvent<ReagentGrinderComponent, PowerChangedEvent>((_, component, _) => EnqueueUiUpdate(component));
            SubscribeLocalEvent<ReagentGrinderComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<StackComponent, ExtractableScalingEvent>(ExtractableScaling);

            SubscribeLocalEvent<ReagentGrinderComponent, ReagentGrinderGrindStartMessage>(OnGrindStartMessage);
            SubscribeLocalEvent<ReagentGrinderComponent, ReagentGrinderJuiceStartMessage>(OnJuiceStartMessage);
            SubscribeLocalEvent<ReagentGrinderComponent, ReagentGrinderEjectChamberAllMessage>(OnEjectChamberAllMessage);
            SubscribeLocalEvent<ReagentGrinderComponent, ReagentGrinderEjectChamberContentMessage>(OnEjectChamberContentMessage);

            SubscribeLocalEvent<ReagentGrinderComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
            SubscribeLocalEvent<ReagentGrinderComponent, EntRemovedFromContainerMessage>(OnContainerModified);
            SubscribeLocalEvent<ReagentGrinderComponent, ContainerIsRemovingAttemptEvent>(OnEntRemoveAttempt);
        }

        private void OnEntRemoveAttempt(EntityUid uid, ReagentGrinderComponent component, ContainerIsRemovingAttemptEvent args)
        {
            if (component.Busy)
                args.Cancel();
        }

        private void OnContainerModified(EntityUid uid, ReagentGrinderComponent component, ContainerModifiedMessage args)
        {
            EnqueueUiUpdate(component);

            if (args.Container.ID != component.BeakerSlotId)
                return;

            _appearance.SetData(uid, ReagentGrinderVisualState.BeakerAttached, component.BeakerSlot.HasItem);

            component.BeakerSolution = null;
            if (component.BeakerSlot.Item != null)
                _solutionsSystem.TryGetFitsInDispenser(component.BeakerSlot.Item.Value, out component.BeakerSolution);
        }

        private void ExtractableScaling(EntityUid uid, StackComponent component, ExtractableScalingEvent args)
        {
            args.Scalar *= component.Count; // multiply scalar by amount of items in stack
        }

        private void OnInteractUsing(EntityUid uid, ReagentGrinderComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            var heldEnt = args.Used;

            //See if the user is trying to insert something they want to be ground/juiced.
            if (!HasComp<ExtractableComponent>(heldEnt))
            {
                //Entity did NOT pass the whitelist for grind/juice.
                //Wouldn't want the clown grinding up the Captain's ID card now would you?
                //Why am I asking you? You're biased.
                return;
            }

            //Cap the chamber. Don't want someone putting in 500 entities and ejecting them all at once.
            //Maybe I should have done that for the microwave too?
            if (component.Chamber.ContainedEntities.Count >= component.StorageCap)
                return;

            if (!component.Chamber.Insert(heldEnt, EntityManager))
                return;

            args.Handled = true;
        }

        private void EnqueueUiUpdate(ReagentGrinderComponent component)
        {
            if (!_uiUpdateQueue.Contains(component))
                _uiUpdateQueue.Enqueue(component);
        }

        private void OnComponentInit(EntityUid uid, ReagentGrinderComponent component, ComponentInit args)
        {
            EnqueueUiUpdate(component);

            _itemSlotsSystem.AddItemSlot(uid, component.BeakerSlotId, component.BeakerSlot);

            //A container for the things that WILL be ground/juiced. Useful for ejecting them instead of deleting them from the hands of the user.
            component.Chamber =
                _container.EnsureContainer<Container>(uid, "ReagentGrinderComponent=entityContainerContainer");
        }

        private void OnComponentRemove(EntityUid uid, ReagentGrinderComponent component, ComponentRemove args)
        {
            _itemSlotsSystem.RemoveItemSlot(uid, component.BeakerSlot);
        }

        private void OnGrindStartMessage(EntityUid uid, ReagentGrinderComponent component, ReagentGrinderGrindStartMessage msg)
        {
            if (component.Busy || msg.Session.AttachedEntity is null)
                return;

            if (!this.IsPowered(uid, EntityManager))
                return;
            ClickSound(component);
            DoWork(component, GrinderProgram.Grind);
        }

        private void OnJuiceStartMessage(EntityUid uid, ReagentGrinderComponent component, ReagentGrinderJuiceStartMessage msg)
        {
            if (component.Busy || msg.Session.AttachedEntity is null)
                return;

            if (!this.IsPowered(uid, EntityManager))
                return;
            ClickSound(component);
            DoWork(component, GrinderProgram.Juice);
        }

        private void OnEjectChamberAllMessage(EntityUid uid, ReagentGrinderComponent component, ReagentGrinderEjectChamberAllMessage msg)
        {
            if (component.Busy || msg.Session.AttachedEntity is null)
                return;

            if (component.Chamber.ContainedEntities.Count <= 0)
                return;
            ClickSound(component);
            for (var i = component.Chamber.ContainedEntities.Count - 1; i >= 0; i--)
            {
                var entity = component.Chamber.ContainedEntities[i];
                component.Chamber.Remove(entity);
                var xform = Transform(entity);
                xform.Coordinates = xform.Coordinates.Offset(_random.NextVector2(0.4f));
            }
            EnqueueUiUpdate(component);
        }

        private void OnEjectChamberContentMessage(EntityUid uid, ReagentGrinderComponent component, ReagentGrinderEjectChamberContentMessage msg)
        {
            if (component.Busy || msg.Session.AttachedEntity is null)
                return;

            if (!component.Chamber.ContainedEntities.TryFirstOrNull(x => x == msg.EntityId, out var ent))
                return;
            component.Chamber.Remove(ent.Value);
            var xform = Transform(ent.Value);
            xform.Coordinates = xform.Coordinates.Offset(_random.NextVector2(0.4f));
            EnqueueUiUpdate(component);
            ClickSound(component);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            while (_uiUpdateQueue.TryDequeue(out var comp))
            {
                if (comp.Deleted)
                    continue;

                var canJuice = false;
                var canGrind = false;
                if (comp.BeakerSlot.HasItem)
                {
                    foreach (var entity in comp.Chamber.ContainedEntities)
                    {
                        if (canJuice || !EntityManager.TryGetComponent(entity, out ExtractableComponent? component)) continue;

                        canJuice = component.JuiceSolution != null;
                        canGrind = component.GrindableSolution != null
                                   && _solutionsSystem.TryGetSolution(entity, component.GrindableSolution, out _);
                    }
                }

                var state = new ReagentGrinderInterfaceState
                (
                    comp.Busy,
                    comp.BeakerSlot.HasItem,
                    this.IsPowered(comp.Owner, EntityManager),
                    canJuice,
                    canGrind,
                    comp.Chamber.ContainedEntities.Select(item => item).ToArray(),
                    //Remember the beaker can be null!
                    comp.BeakerSolution?.Contents.ToArray()
                );
                if (!_ui.TryGetUi(comp.Owner, ReagentGrinderUiKey.Key, out var bui))
                    continue;
                _ui.SetUiState(bui, state);
            }
        }

        /// <summary>
        /// The wzhzhzh of the grinder. Processes the contents of the grinder and puts the output in the beaker.
        /// </summary>
        /// <param name="component"></param>
        /// <param name="program">true for wanting to juice, false for wanting to grind.</param>
        private void DoWork(ReagentGrinderComponent component, GrinderProgram program)
        {
            //Have power, are  we busy, chamber has anything to grind, a beaker for the grounds to go?
            if (!this.IsPowered(component.Owner, EntityManager) ||
                component.Busy || component.Chamber.ContainedEntities.Count <= 0 ||
                component.BeakerSlot.Item is not { } beakerEntity ||
                component.BeakerSolution == null)
            {
                return;
            }

            component.Busy = true;

            if (!_ui.TryGetUi(component.Owner, ReagentGrinderUiKey.Key, out var bui))
                return;
            _ui.SendUiMessage(bui, new ReagentGrinderWorkStartedMessage(program));
            switch (program)
            {
                case GrinderProgram.Grind:
                    _audio.PlayPvs(component.GrindSound, component.Owner);

                    // Get each item inside the chamber and get the reagents it contains.
                    // Transfer those reagents to the beaker, given we have one in.
                    component.Owner.SpawnTimer(component.WorkTime, () =>
                    {
                        foreach (var item in component.Chamber.ContainedEntities.ToList())
                        {
                            if (!EntityManager.TryGetComponent(item, out ExtractableComponent? extract)
                                || extract.GrindableSolution == null
                                || !_solutionsSystem.TryGetSolution(item, extract.GrindableSolution, out var solution)) continue;

                            var juiceEvent = new ExtractableScalingEvent(); // default of scalar is always 1.0
                            RaiseLocalEvent(item, juiceEvent);

                            if (component.BeakerSolution.CurrentVolume + solution.CurrentVolume * juiceEvent.Scalar >
                                component.BeakerSolution.MaxVolume)
                                continue;

                            solution.ScaleSolution(juiceEvent.Scalar);
                            _solutionsSystem.TryAddSolution(beakerEntity, component.BeakerSolution, solution);
                            EntityManager.DeleteEntity(item);
                        }

                        component.Busy = false;
                        EnqueueUiUpdate(component);
                        _ui.SendUiMessage(bui, new ReagentGrinderWorkCompleteMessage());
                    });
                    break;

                case GrinderProgram.Juice:
                    _audio.PlayPvs(component.JuiceSound, component.Owner);
                    component.Owner.SpawnTimer(component.WorkTime, () =>
                    {
                        foreach (var item in component.Chamber.ContainedEntities.ToList())
                        {
                            if (!EntityManager.TryGetComponent<ExtractableComponent?>(item, out var juiceMe)
                                || juiceMe.JuiceSolution == null)
                            {
                                Logger.Warning("Couldn't find a juice solution on entityUid:{0}", item);
                                continue;
                            }
                            var juiceEvent = new ExtractableScalingEvent(); // default of scalar is always 1.0
                            if (EntityManager.HasComponent<StackComponent>(item))
                            {
                                RaiseLocalEvent(item, juiceEvent, true);
                            }

                            if (component.BeakerSolution.CurrentVolume + juiceMe.JuiceSolution.TotalVolume * juiceEvent.Scalar > component.BeakerSolution.MaxVolume)
                                continue;
                            juiceMe.JuiceSolution.ScaleSolution(juiceEvent.Scalar);
                            _solutionsSystem.TryAddSolution(beakerEntity, component.BeakerSolution, juiceMe.JuiceSolution);
                            EntityManager.DeleteEntity(item);
                        }

                        _ui.SendUiMessage(bui, new ReagentGrinderWorkCompleteMessage());
                        component.Busy = false;
                        EnqueueUiUpdate(component);
                    });
                    break;
            }
        }

        private void ClickSound(ReagentGrinderComponent component)
        {
            _audio.PlayPvs(component.ClickSound, component.Owner, AudioParams.Default.WithVolume(-2));
        }
    }
}
