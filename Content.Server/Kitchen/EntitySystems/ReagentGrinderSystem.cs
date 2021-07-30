using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Chemistry.Components;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Kitchen.Components;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Chemistry.Solution;
using Content.Shared.Interaction;
using Content.Shared.Kitchen.Components;
using Content.Shared.Notification.Managers;
using Content.Shared.Random.Helpers;
using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Kitchen.EntitySystems
{
    [UsedImplicitly]
    internal sealed class ReagentGrinderSystem : EntitySystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private Queue<ReagentGrinderComponent> _uiUpdateQueue = new ();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ReagentGrinderComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<ReagentGrinderComponent, PowerChangedEvent>((_, component, _) => EnqueueUiUpdate(component));
            SubscribeLocalEvent<ReagentGrinderComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<ReagentGrinderComponent, InteractUsingEvent>(OnInteractUsing);
        }

        private void OnInteractUsing(EntityUid uid, ReagentGrinderComponent component, InteractUsingEvent args)
        {
            if(args.Handled) return;

            if (!args.User.TryGetComponent(out IHandsComponent? hands))
            {
                component.Owner.PopupMessage(args.User, Loc.GetString("reagent-grinder-component-interact-using-no-hands"));
                args.Handled = true;
                return;
            }

            IEntity heldEnt = args.Used;

            //First, check if user is trying to insert a beaker.
            //No promise it will be a beaker right now, but whatever.
            //Maybe this should whitelist "beaker" in the prototype id of heldEnt?
            if(heldEnt.TryGetComponent(out SolutionContainerComponent? beaker) && beaker.Capabilities.HasFlag(SolutionContainerCaps.FitsInDispenser))
            {
                component.BeakerContainer.Insert(heldEnt);
                component.HeldBeaker = beaker;
                EnqueueUiUpdate(component);
                //We are done, return. Insert the beaker and exit!
                if (component.Owner.TryGetComponent(out AppearanceComponent? appearance))
                {
                    appearance.SetData(SharedReagentGrinderComponent.ReagentGrinderVisualState.BeakerAttached, component.BeakerContainer.ContainedEntity != null);
                }
                ClickSound(component);
                args.Handled = true;
                return;
            }

            //Next, see if the user is trying to insert something they want to be ground/juiced.
            if(!heldEnt.HasTag("Grindable") && !heldEnt.TryGetComponent(out JuiceableComponent? juice))
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

            if (!args.User.TryGetComponent(out ActorComponent? actor))
            {
                return;
            }
            EnqueueUiUpdate(component);
            component.Owner.GetUIOrNull(SharedReagentGrinderComponent.ReagentGrinderUiKey.Key)?.Toggle(actor.PlayerSession);
            args.Handled = true;
        }

        private void EnqueueUiUpdate(ReagentGrinderComponent component)
        {
            if(!_uiUpdateQueue.Contains(component)) _uiUpdateQueue.Enqueue(component);
        }

        private void OnComponentInit(EntityUid uid, ReagentGrinderComponent component, ComponentInit args)
        {
            EnqueueUiUpdate(component);

            //A slot for the beaker where the grounds/juices will go.
            component.BeakerContainer =
                ContainerHelpers.EnsureContainer<ContainerSlot>(component.Owner, $"{component.Name}-reagentContainerContainer");

            //A container for the things that WILL be ground/juiced. Useful for ejecting them instead of deleting them from the hands of the user.
            component.Chamber =
                ContainerHelpers.EnsureContainer<Container>(component.Owner, $"{component.Name}-entityContainerContainer");

            var bui = component.Owner.GetUIOrNull(SharedReagentGrinderComponent.ReagentGrinderUiKey.Key);
            if (bui != null)
            {
                bui.OnReceiveMessage += msg => OnUIMessageReceived(uid, component, msg);
            }
        }

        private void OnUIMessageReceived(EntityUid uid, ReagentGrinderComponent component,
            ServerBoundUserInterfaceMessage message)
        {
            if(component.Busy)
            {
                return;
            }

            switch(message.Message)
            {
                case SharedReagentGrinderComponent.ReagentGrinderGrindStartMessage msg:
                    if (!component.Owner.TryGetComponent(out ApcPowerReceiverComponent? receiver) || !receiver.Powered) break;
                    ClickSound(component);
                    DoWork(component, message.Session.AttachedEntity!, SharedReagentGrinderComponent.GrinderProgram.Grind);
                    break;

                case SharedReagentGrinderComponent.ReagentGrinderJuiceStartMessage msg:
                    if (!component.Owner.TryGetComponent(out ApcPowerReceiverComponent? receiver2) || !receiver2.Powered) break;
                    ClickSound(component);
                    DoWork(component, message.Session.AttachedEntity!, SharedReagentGrinderComponent.GrinderProgram.Juice);
                    break;

                case SharedReagentGrinderComponent.ReagentGrinderEjectChamberAllMessage msg:
                    if(component.Chamber.ContainedEntities.Count > 0)
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
                    if (component.Chamber.ContainedEntities.TryFirstOrDefault(x => x.Uid == msg.EntityID, out var ent))
                    {
                        component.Chamber.Remove(ent);
                        ent.RandomOffset(0.4f);
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
                bool canJuice = false;
                bool canGrind = false;
                if (comp.BeakerContainer.ContainedEntity != null)
                {
                    foreach (var entity in comp.Chamber.ContainedEntities)
                    {
                        if (!canJuice && entity.HasComponent<JuiceableComponent>()) canJuice = true;
                        if (!canGrind && entity.HasTag("Grindable")) canGrind = true;
                        if (canJuice && canGrind) break;
                    }
                }

                comp.Owner.GetUIOrNull(SharedReagentGrinderComponent.ReagentGrinderUiKey.Key)?.SetState(new ReagentGrinderInterfaceState
                (
                    comp.Busy,
                    comp.BeakerContainer.ContainedEntity != null,
                    comp.Owner.TryGetComponent(out ApcPowerReceiverComponent? receiver) && receiver.Powered,
                    canJuice,
                    canGrind,
                    comp.Chamber.ContainedEntities.Select(item => item.Uid).ToArray(),
                    //Remember the beaker can be null!
                    comp.HeldBeaker?.Solution.Contents.ToArray()
                ));
            }
        }

        /// <summary>
        /// Tries to eject whatever is in the beaker slot. Puts the item in the user's hands or failing that on top
        /// of the grinder.
        /// </summary>
        private void EjectBeaker(ReagentGrinderComponent component, IEntity? user)
        {
            if (component.BeakerContainer.ContainedEntity == null || component.HeldBeaker == null || component.Busy)
                return;

            var beaker = component.BeakerContainer.ContainedEntity;
            if(beaker is null)
                return;

            component.BeakerContainer.Remove(beaker);

            if (user == null || !user.TryGetComponent<HandsComponent>(out var hands) || !component.HeldBeaker.Owner.TryGetComponent<ItemComponent>(out var item))
                return;
            hands.PutInHandOrDrop(item);

            component.HeldBeaker = null;
            EnqueueUiUpdate(component);
            if (component.Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(SharedReagentGrinderComponent.ReagentGrinderVisualState.BeakerAttached, component.BeakerContainer.ContainedEntity != null);
            }
        }

        /// <summary>
        /// The wzhzhzh of the grinder. Processes the contents of the grinder and puts the output in the beaker.
        /// </summary>
        /// <param name="isJuiceIntent">true for wanting to juice, false for wanting to grind.</param>
        private void DoWork(ReagentGrinderComponent component, IEntity user, SharedReagentGrinderComponent.GrinderProgram program)
        {
            //Have power, are  we busy, chamber has anything to grind, a beaker for the grounds to go?
            if(!component.Owner.TryGetComponent(out ApcPowerReceiverComponent? receiver) || !receiver.Powered || component.Busy || component.Chamber.ContainedEntities.Count <= 0 || component.BeakerContainer.ContainedEntity == null || component.HeldBeaker == null)
            {
                return;
            }

            component.Busy = true;

            var bui = component.Owner.GetUIOrNull(SharedReagentGrinderComponent.ReagentGrinderUiKey.Key);
            bui?.SendMessage(new SharedReagentGrinderComponent.ReagentGrinderWorkStartedMessage(program));
            switch (program)
            {
                case SharedReagentGrinderComponent.GrinderProgram.Grind:
                    SoundSystem.Play(Filter.Pvs(component.Owner), "/Audio/Machines/blender.ogg", component.Owner, AudioParams.Default);
                    //Get each item inside the chamber and get the reagents it contains. Transfer those reagents to the beaker, given we have one in.
                    component.Owner.SpawnTimer(component.WorkTime, (Action) (() =>
                    {
                        foreach (var item in component.Chamber.ContainedEntities.ToList())
                        {
                            if (!item.HasTag("Grindable")) continue;
                            if (!item.TryGetComponent<SolutionContainerComponent>(out var solution)) continue;
                            if (component.HeldBeaker.CurrentVolume + solution.CurrentVolume > component.HeldBeaker.MaxVolume) continue;
                            component.HeldBeaker.TryAddSolution(solution.Solution);
                            solution.RemoveAllSolution();
                            item.Delete();
                        }

                        component.Busy = false;
                        EnqueueUiUpdate(component);
                        bui?.SendMessage(new SharedReagentGrinderComponent.ReagentGrinderWorkCompleteMessage());
                    }));
                    break;

                case SharedReagentGrinderComponent.GrinderProgram.Juice:
                    SoundSystem.Play(Filter.Pvs(component.Owner), "/Audio/Machines/juicer.ogg", component.Owner, AudioParams.Default);
                    component.Owner.SpawnTimer(component.WorkTime, (Action) (() =>
                    {
                        foreach (var item in component.Chamber.ContainedEntities.ToList())
                        {
                            if (!item.TryGetComponent<JuiceableComponent>(out var juiceMe)) continue;
                            if (component.HeldBeaker.CurrentVolume + juiceMe.JuiceResultSolution.TotalVolume > component.HeldBeaker.MaxVolume) continue;
                            component.HeldBeaker.TryAddSolution(juiceMe.JuiceResultSolution);
                            item.Delete();
                        }
                        bui?.SendMessage(new SharedReagentGrinderComponent.ReagentGrinderWorkCompleteMessage());
                        component.Busy = false;
                        EnqueueUiUpdate(component);
                    }));
                    break;
            }
        }

        private void ClickSound(ReagentGrinderComponent component)
        {
            SoundSystem.Play(Filter.Pvs(component.Owner), "/Audio/Machines/machine_switch.ogg", component.Owner, AudioParams.Default.WithVolume(-2f));
        }
    }
}
