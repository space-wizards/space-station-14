using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Chemistry.Components;
using Content.Server.Kitchen.Components;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Kitchen.Components;
using Content.Shared.Random.Helpers;
using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
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
        }

        private void EnqueueUiUpdate(ReagentGrinderComponent component)
        {
            if(_uiUpdateQueue.Contains(component)) _uiUpdateQueue.Enqueue(component);
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
                    if (component.Chamber.ContainedEntities.TryFirstOrDefault(x => x == message.Session.AttachedEntity, out var enti))
                    {
                        ClickSound(component);
                        component.Chamber.Remove(enti);
                        enti.RandomOffset(0.4f);
                        EnqueueUiUpdate(component);
                    }

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
        /// The wzhzhzh of the grinder. Processes the contents of the grinder and puts the output in the beaker.
        /// </summary>
        /// <param name="isJuiceIntent">true for wanting to juice, false for wanting to grind.</param>
        private void DoWork(ReagentGrinderComponent component, IEntity user, SharedReagentGrinderComponent.GrinderProgram program)
        {
            //Have power, are  we busy, chamber has anything to grind, a beaker for the grounds to go?
            if(!component.Owner.TryGetComponent(out ApcPowerReceiverComponent? receiver) || !receiver.Powered || component.Busy || component.Chamber.ContainedEntities.Count <= 0 || !component.BeakerContainer.ContainedEntity != null || component.HeldBeaker == null)
            {
                return;
            }

            component.Busy = true;

            var bui = component.Owner.GetUIOrNull(SharedReagentGrinderComponent.ReagentGrinderUiKey.Key);
            bui?.SendMessage(new SharedReagentGrinderComponent.ReagentGrinderWorkStartedMessage(program));
            switch (program)
            {
                case SharedReagentGrinderComponent.GrinderProgram.Grind:
                    SoundSystem.Play(Filter.Pvs(component.Owner), "/Audio/Machines/blender.ogg", Owner, AudioParams.Default);
                    //Get each item inside the chamber and get the reagents it contains. Transfer those reagents to the beaker, given we have one in.
                    component.Owner.SpawnTimer(_workTime, (Action) (() =>
                    {
                        foreach (var item in _chamber.ContainedEntities.ToList())
                        {
                            if (!item.HasTag("Grindable")) continue;
                            if (!item.TryGetComponent<SolutionContainerComponent>(out var solution)) continue;
                            if (_heldBeaker.CurrentVolume + solution.CurrentVolume > _heldBeaker.MaxVolume) continue;
                            _heldBeaker.TryAddSolution(solution.Solution);
                            solution.RemoveAllSolution();
                            item.Delete();
                        }

                        component.Busy = false;
                        EnqueueUiUpdate(component);
                        bui?.SendMessage(new SharedReagentGrinderComponent.ReagentGrinderWorkCompleteMessage());
                    }));
                    break;

                case SharedReagentGrinderComponent.GrinderProgram.Juice:
                    SoundSystem.Play(Filter.Pvs(component.Owner), "/Audio/Machines/juicer.ogg", Owner, AudioParams.Default);
                    component.Owner.SpawnTimer(_workTime, (Action) (() =>
                    {
                        foreach (var item in _chamber.ContainedEntities.ToList())
                        {
                            if (!item.TryGetComponent<JuiceableComponent>(out var juiceMe)) continue;
                            if (_heldBeaker.CurrentVolume + juiceMe.JuiceResultSolution.TotalVolume > _heldBeaker.MaxVolume) continue;
                            _heldBeaker.TryAddSolution(juiceMe.JuiceResultSolution);
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
