using System.Collections.Generic;
using System.Linq;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Kitchen.Components;
using Content.Server.Kitchen.Events;
using Content.Server.Power.Components;
using Content.Server.Stack;
using Content.Server.UserInterface;
using Content.Shared.Interaction;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Kitchen.EntitySystems
{
    [UsedImplicitly]
    internal sealed class ReagentGrinderSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionsSystem = default!;

        private Queue<ReagentGrinderComponent> _uiUpdateQueue = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ReagentGrinderComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<ReagentGrinderComponent, PowerChangedEvent>((_, component, _) =>
                EnqueueUiUpdate(component));
            SubscribeLocalEvent<ReagentGrinderComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<ReagentGrinderComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<StackComponent, ExtractableScalingEvent>(ExtractableScaling);
        }

        private void ExtractableScaling(EntityUid uid, StackComponent component, ExtractableScalingEvent args)
        {
            args.Scalar *= component.Count; // multiply scalar by amount of items in stack
        }

        private void OnInteractUsing(EntityUid uid, ReagentGrinderComponent component, InteractUsingEvent args)
        {
            if (args.Handled) return;

            if (!EntityManager.HasComponent<HandsComponent>(args.User))
            {
                component.Owner.PopupMessage(args.User,
                    Loc.GetString("reagent-grinder-component-interact-using-no-hands"));
                args.Handled = true;
                return;
            }

            var heldEnt = args.Used;

            // First, check if user is trying to insert a beaker.
            // No promise it will be a beaker right now, but whatever.
            // Maybe this should whitelist "beaker" in the prototype id of heldEnt?
            if (_solutionsSystem.TryGetFitsInDispenser(heldEnt, out var beaker))
            {
                component.BeakerContainer.Insert(heldEnt);
                component.HeldBeaker = beaker;
                EnqueueUiUpdate(component);
                //We are done, return. Insert the beaker and exit!
                if (EntityManager.TryGetComponent(component.Owner, out AppearanceComponent? appearance))
                {
                    appearance.SetData(SharedReagentGrinderComponent.ReagentGrinderVisualState.BeakerAttached,
                        component.BeakerContainer.ContainedEntity != null);
                }

                ClickSound(component);
                args.Handled = true;
                return;
            }

            //Next, see if the user is trying to insert something they want to be ground/juiced.
            if (!EntityManager.TryGetComponent(heldEnt, out ExtractableComponent? juice))
            {
                //Entity did NOT pass the whitelist for grind/juice.
                //Wouldn't want the clown grinding up the Captain's ID card now would you?
                //Why am I asking you? You're biased.
                return;
            }

            //Cap the chamber. Don't want someone putting in 500 entities and ejecting them all at once.
            //Maybe I should have done that for the microwave too?
            if (component.Chamber.ContainedEntities.Count >= component.StorageCap)
            {
                return;
            }

            if (!component.Chamber.Insert(heldEnt))
            {
                return;
            }

            EnqueueUiUpdate(component);
            args.Handled = true;
        }

        private void OnInteractHand(EntityUid uid, ReagentGrinderComponent component, InteractHandEvent args)
        {
            if (args.Handled) return;

            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
            {
                return;
            }

            EnqueueUiUpdate(component);
            component.Owner.GetUIOrNull(SharedReagentGrinderComponent.ReagentGrinderUiKey.Key)
                ?.Toggle(actor.PlayerSession);
            args.Handled = true;
        }

        private void EnqueueUiUpdate(ReagentGrinderComponent component)
        {
            if (!_uiUpdateQueue.Contains(component)) _uiUpdateQueue.Enqueue(component);
        }

        private void OnComponentInit(EntityUid uid, ReagentGrinderComponent component, ComponentInit args)
        {
            EnqueueUiUpdate(component);

            //A slot for the beaker where the grounds/juices will go.
            component.BeakerContainer =
                ContainerHelpers.EnsureContainer<ContainerSlot>(component.Owner,
                    $"{component.Name}-reagentContainerContainer");

            //A container for the things that WILL be ground/juiced. Useful for ejecting them instead of deleting them from the hands of the user.
            component.Chamber =
                ContainerHelpers.EnsureContainer<Container>(component.Owner,
                    $"{component.Name}-entityContainerContainer");

            var bui = component.Owner.GetUIOrNull(SharedReagentGrinderComponent.ReagentGrinderUiKey.Key);
            if (bui != null)
            {
                bui.OnReceiveMessage += msg => OnUIMessageReceived(uid, component, msg);
            }
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
                    if (!EntityManager.TryGetComponent(component.Owner, out ApcPowerReceiverComponent? receiver) ||
                        !receiver.Powered) break;
                    ClickSound(component);
                    DoWork(component, attached,
                        SharedReagentGrinderComponent.GrinderProgram.Grind);
                    break;

                case SharedReagentGrinderComponent.ReagentGrinderJuiceStartMessage msg:
                    if (!EntityManager.TryGetComponent(component.Owner, out ApcPowerReceiverComponent? receiver2) ||
                        !receiver2.Powered) break;
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

                case SharedReagentGrinderComponent.ReagentGrinderEjectBeakerMessage msg:
                    ClickSound(component);
                    EjectBeaker(component, message.Session.AttachedEntity);
                    EnqueueUiUpdate(component);
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
                if (comp.BeakerContainer.ContainedEntity != null)
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
                        comp.BeakerContainer.ContainedEntity != null,
                        EntityManager.TryGetComponent(comp.Owner, out ApcPowerReceiverComponent? receiver) && receiver.Powered,
                        canJuice,
                        canGrind,
                        comp.Chamber.ContainedEntities.Select(item => item).ToArray(),
                        //Remember the beaker can be null!
                        comp.HeldBeaker?.Contents.ToArray()
                    ));
            }
        }

        /// <summary>
        /// Tries to eject whatever is in the beaker slot. Puts the item in the user's hands or failing that on top
        /// of the grinder.
        /// </summary>
        private void EjectBeaker(ReagentGrinderComponent component, EntityUid? user)
        {
            if (component.BeakerContainer.ContainedEntity == null || component.HeldBeaker == null || component.Busy)
                return;

            if (component.BeakerContainer.ContainedEntity is not {Valid: true} beaker)
                return;

            component.BeakerContainer.Remove(beaker);

            if (user == null ||
                !EntityManager.TryGetComponent<HandsComponent?>(user.Value, out var hands) ||
                !EntityManager.TryGetComponent<ItemComponent?>(beaker, out var item))
                return;

            hands.PutInHandOrDrop(item);

            component.HeldBeaker = null;
            EnqueueUiUpdate(component);
            if (EntityManager.TryGetComponent(component.Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(SharedReagentGrinderComponent.ReagentGrinderVisualState.BeakerAttached,
                    component.BeakerContainer.ContainedEntity != null);
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
            if (!EntityManager.TryGetComponent(component.Owner, out ApcPowerReceiverComponent? receiver) || !receiver.Powered ||
                component.Busy || component.Chamber.ContainedEntities.Count <= 0 ||
                component.BeakerContainer.ContainedEntity == null || component.HeldBeaker == null)
            {
                return;
            }

            component.Busy = true;

            var bui = component.Owner.GetUIOrNull(SharedReagentGrinderComponent.ReagentGrinderUiKey.Key);
            bui?.SendMessage(new SharedReagentGrinderComponent.ReagentGrinderWorkStartedMessage(program));
            var beakerEntity = component.BeakerContainer.ContainedEntity;
            switch (program)
            {
                case SharedReagentGrinderComponent.GrinderProgram.Grind:
                    SoundSystem.Play(Filter.Pvs(component.Owner), component.GrindSound.GetSound(), component.Owner, AudioParams.Default);
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
                            if (component.HeldBeaker.CurrentVolume + solution.CurrentVolume * juiceEvent.Scalar >
                                component.HeldBeaker.MaxVolume) continue;
                            solution.ScaleSolution(juiceEvent.Scalar);
                            _solutionsSystem.TryAddSolution(beakerEntity.Value, component.HeldBeaker, solution);
                            _solutionsSystem.RemoveAllSolution(beakerEntity.Value, solution);
                            EntityManager.DeleteEntity(item);
                        }

                        component.Busy = false;
                        EnqueueUiUpdate(component);
                        bui?.SendMessage(new SharedReagentGrinderComponent.ReagentGrinderWorkCompleteMessage());
                    });
                    break;

                case SharedReagentGrinderComponent.GrinderProgram.Juice:
                    SoundSystem.Play(Filter.Pvs(component.Owner), component.JuiceSound.GetSound(), component.Owner, AudioParams.Default);
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
                                RaiseLocalEvent(item, juiceEvent);
                            }

                            if (component.HeldBeaker.CurrentVolume +
                                juiceMe.JuiceSolution.TotalVolume * juiceEvent.Scalar >
                                component.HeldBeaker.MaxVolume) continue;
                            juiceMe.JuiceSolution.ScaleSolution(juiceEvent.Scalar);
                            _solutionsSystem.TryAddSolution(beakerEntity.Value, component.HeldBeaker, juiceMe.JuiceSolution);
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
            SoundSystem.Play(Filter.Pvs(component.Owner), component.ClickSound.GetSound(), component.Owner, AudioParams.Default.WithVolume(-2f));
        }
    }
}
