using System.Linq;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Kitchen.Components;
using Content.Server.Kitchen.Events;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Stack;
using Content.Server.UserInterface;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Kitchen.Components;
using Content.Shared.Random.Helpers;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Kitchen.EntitySystems
{
    [UsedImplicitly]
    internal sealed class ReagentGrinderSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionsSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;

        private Queue<ReagentGrinderComponent> _uiUpdateQueue = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ReagentGrinderComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<ReagentGrinderComponent, ComponentRemove>(OnComponentRemove);

            SubscribeLocalEvent<ReagentGrinderComponent, PowerChangedEvent>(OnPowerChange);
            SubscribeLocalEvent<ReagentGrinderComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<StackComponent, ExtractableScalingEvent>(ExtractableScaling);

            SubscribeLocalEvent<ReagentGrinderComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
            SubscribeLocalEvent<ReagentGrinderComponent, EntRemovedFromContainerMessage>(OnContainerModified);
            SubscribeLocalEvent<ReagentGrinderComponent, ContainerIsRemovingAttemptEvent>(OnEntRemoveAttempt);
        }

        private void OnPowerChange(EntityUid uid, ReagentGrinderComponent component, ref PowerChangedEvent args)
        {
            EnqueueUiUpdate(component);
        }

        private void OnEntRemoveAttempt(EntityUid uid, ReagentGrinderComponent component, ContainerIsRemovingAttemptEvent args)
        {
            if (component.Busy)
                args.Cancel();
        }

        private void OnContainerModified(EntityUid uid, ReagentGrinderComponent component, ContainerModifiedMessage args)
        {
            EnqueueUiUpdate(component);

            if (args.Container.ID != SharedReagentGrinderComponent.BeakerSlotId)
                return;

            if (TryComp(component.Owner, out AppearanceComponent? appearance))
                appearance.SetData(SharedReagentGrinderComponent.ReagentGrinderVisualState.BeakerAttached, component.BeakerSlot.HasItem);

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
            if (args.Handled) return;

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
            if (!_uiUpdateQueue.Contains(component)) _uiUpdateQueue.Enqueue(component);
        }

        private void OnComponentInit(EntityUid uid, ReagentGrinderComponent component, ComponentInit args)
        {
            EnqueueUiUpdate(component);

            _itemSlotsSystem.AddItemSlot(uid, SharedReagentGrinderComponent.BeakerSlotId, component.BeakerSlot);

            //A container for the things that WILL be ground/juiced. Useful for ejecting them instead of deleting them from the hands of the user.
            component.Chamber =
                ContainerHelpers.EnsureContainer<Container>(component.Owner,
                    $"{component.Name}-entityContainerContainer");

            // TODO just directly subscribe to UI events.
            var bui = component.Owner.GetUIOrNull(SharedReagentGrinderComponent.ReagentGrinderUiKey.Key);
            if (bui != null)
            {
                bui.OnReceiveMessage += msg => OnUIMessageReceived(uid, component, msg);
            }
        }

        private void OnComponentRemove(EntityUid uid, ReagentGrinderComponent component, ComponentRemove args)
        {
            _itemSlotsSystem.RemoveItemSlot(uid, component.BeakerSlot);
        }

        private void OnUIMessageReceived(EntityUid uid, ReagentGrinderComponent component,
            ServerBoundUserInterfaceMessage message)
        {
            if (component.Busy || message.Session.AttachedEntity is not {} attached)
            {
                return;
            }

            switch (message.Message)
            {
                case SharedReagentGrinderComponent.ReagentGrinderGrindStartMessage msg:
                    if (!this.IsPowered(component.Owner, EntityManager)) break;
                    ClickSound(component);
                    DoWork(component, attached,
                        SharedReagentGrinderComponent.GrinderProgram.Grind);
                    break;

                case SharedReagentGrinderComponent.ReagentGrinderJuiceStartMessage msg:
                    if (!this.IsPowered(component.Owner, EntityManager)) break;
                    ClickSound(component);
                    DoWork(component, attached,
                        SharedReagentGrinderComponent.GrinderProgram.Juice);
                    break;

                case SharedReagentGrinderComponent.ReagentGrinderEjectChamberAllMessage msg:
                    if (component.Chamber.ContainedEntities.Count > 0)
                    {
                        ClickSound(component);
                        for (var i = component.Chamber.ContainedEntities.Count - 1; i >= 0; i--)
                        {
                            var entity = component.Chamber.ContainedEntities[i];
                            component.Chamber.Remove(entity);
                            entity.RandomOffset(0.4f);
                        }

                        EnqueueUiUpdate(component);
                    }

                    break;

                case SharedReagentGrinderComponent.ReagentGrinderEjectChamberContentMessage msg:
                    if (component.Chamber.ContainedEntities.TryFirstOrNull(x => x == msg.EntityID, out var ent))
                    {
                        component.Chamber.Remove(ent.Value);
                        SharedEntityExtensions.RandomOffset(ent.Value, 0.4f);
                        EnqueueUiUpdate(component);
                        ClickSound(component);
                    }

                    break;
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            while (_uiUpdateQueue.TryDequeue(out var comp))
            {
                if (comp.Deleted)
                    continue;

                bool canJuice = false;
                bool canGrind = false;
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

                comp.Owner.GetUIOrNull(SharedReagentGrinderComponent.ReagentGrinderUiKey.Key)?.SetState(
                    new ReagentGrinderInterfaceState
                    (
                        comp.Busy,
                        comp.BeakerSlot.HasItem,
                        this.IsPowered(comp.Owner, EntityManager),
                        canJuice,
                        canGrind,
                        comp.Chamber.ContainedEntities.Select(item => item).ToArray(),
                        //Remember the beaker can be null!
                        comp.BeakerSolution?.Contents.ToArray()
                    ));
            }
        }

        /// <summary>
        /// The wzhzhzh of the grinder. Processes the contents of the grinder and puts the output in the beaker.
        /// </summary>
        /// <param name="isJuiceIntent">true for wanting to juice, false for wanting to grind.</param>
        private void DoWork(ReagentGrinderComponent component, EntityUid user,
            SharedReagentGrinderComponent.GrinderProgram program)
        {
            //Have power, are  we busy, chamber has anything to grind, a beaker for the grounds to go?
            if (!this.IsPowered(component.Owner, EntityManager) ||
                component.Busy || component.Chamber.ContainedEntities.Count <= 0 ||
                component.BeakerSlot.Item is not EntityUid beakerEntity ||
                component.BeakerSolution == null)
            {
                return;
            }

            component.Busy = true;

            var bui = component.Owner.GetUIOrNull(SharedReagentGrinderComponent.ReagentGrinderUiKey.Key);
            bui?.SendMessage(new SharedReagentGrinderComponent.ReagentGrinderWorkStartedMessage(program));
            switch (program)
            {
                case SharedReagentGrinderComponent.GrinderProgram.Grind:
                    SoundSystem.Play(component.GrindSound.GetSound(), Filter.Pvs(component.Owner), component.Owner, AudioParams.Default);
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
                            RaiseLocalEvent(item, juiceEvent, false);
                            if (component.BeakerSolution.CurrentVolume + solution.CurrentVolume * juiceEvent.Scalar >
                                component.BeakerSolution.MaxVolume) continue;
                            solution.ScaleSolution(juiceEvent.Scalar);
                            _solutionsSystem.TryAddSolution(beakerEntity, component.BeakerSolution, solution);
                            EntityManager.DeleteEntity(item);
                        }

                        component.Busy = false;
                        EnqueueUiUpdate(component);
                        bui?.SendMessage(new SharedReagentGrinderComponent.ReagentGrinderWorkCompleteMessage());
                    });
                    break;

                case SharedReagentGrinderComponent.GrinderProgram.Juice:
                    SoundSystem.Play(component.JuiceSound.GetSound(), Filter.Pvs(component.Owner), component.Owner, AudioParams.Default);
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

                        bui?.SendMessage(new SharedReagentGrinderComponent.ReagentGrinderWorkCompleteMessage());
                        component.Busy = false;
                        EnqueueUiUpdate(component);
                    });
                    break;
            }
        }

        private void ClickSound(ReagentGrinderComponent component)
        {
            SoundSystem.Play(component.ClickSound.GetSound(), Filter.Pvs(component.Owner), component.Owner, AudioParams.Default.WithVolume(-2f));
        }
    }
}
