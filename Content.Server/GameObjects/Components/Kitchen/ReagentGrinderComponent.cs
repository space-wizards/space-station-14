#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Server.Utility;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Tag;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Kitchen;
using Content.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Kitchen
{

    /// <summary>
    /// The combo reagent grinder/juicer. The reason why grinding and juicing are seperate is simple,
    /// think of grinding as a utility to break an object down into its reagents. Think of juicing as
    /// converting something into its single juice form. E.g, grind an apple and get the nutriment and sugar
    /// it contained, juice an apple and get "apple juice".
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class ReagentGrinderComponent : SharedReagentGrinderComponent, IActivate, IInteractUsing
    {
        private AudioSystem _audioSystem = default!;
        [ViewVariables] private ContainerSlot _beakerContainer = default!;

        /// <summary>
        /// Can be null since we won't always have a beaker in the grinder.
        /// </summary>
        [ViewVariables] private SolutionContainerComponent? _heldBeaker = default!;

        /// <summary>
        /// Contains the things that are going to be ground or juiced.
        /// </summary>
        [ViewVariables] private Container _chamber = default!;

        [ViewVariables] private bool ChamberEmpty => _chamber.ContainedEntities.Count <= 0;
        [ViewVariables] private bool HasBeaker => _beakerContainer.ContainedEntity != null;
        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(ReagentGrinderUiKey.Key);

        private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;

        /// <summary>
        /// Should the BoundUI be told to update?
        /// </summary>
        private bool _uiDirty = true;
        /// <summary>
        /// Is the machine actively doing something and can't be used right now?
        /// </summary>
        private bool _busy = false;

        //YAML serialization vars
        [ViewVariables(VVAccess.ReadWrite)] private int _storageCap = 16;
        [ViewVariables(VVAccess.ReadWrite)] private int _workTime = 3500; //3.5 seconds, completely arbitrary for now.

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _storageCap, "chamberCapacity", 16);
            serializer.DataField(ref _workTime, "workTime", 3500);
        }

        public override void Initialize()
        {
            base.Initialize();
            //A slot for the beaker where the grounds/juices will go.
            _beakerContainer =
                ContainerManagerComponent.Ensure<ContainerSlot>($"{Name}-reagentContainerContainer", Owner);

            //A container for the things that WILL be ground/juiced. Useful for ejecting them instead of deleting them from the hands of the user.
            _chamber =
                ContainerManagerComponent.Ensure<Container>($"{Name}-entityContainerContainer", Owner);

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
            }

            _audioSystem = EntitySystem.Get<AudioSystem>();
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case PowerChangedMessage powerChanged:
                    OnPowerStateChanged(powerChanged);
                    break;
            }
        }

        public override void OnRemove()
        {
            base.OnRemove();
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage -= UserInterfaceOnReceiveMessage;
            }
        }

        private void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage message)
        {
            if(_busy)
            {
                return;
            }

            switch(message.Message)
            {
                case ReagentGrinderGrindStartMessage msg:
                    if (!Powered) break;
                    ClickSound();
                    DoWork(message.Session.AttachedEntity!, GrinderProgram.Grind);
                    break;

                case ReagentGrinderJuiceStartMessage msg:
                    if (!Powered) break;
                    ClickSound();
                    DoWork(message.Session.AttachedEntity!, GrinderProgram.Juice);
                    break;

                case ReagentGrinderEjectChamberAllMessage msg:
                    if(!ChamberEmpty)
                    {
                        ClickSound();
                        for (var i = _chamber.ContainedEntities.Count - 1; i >= 0; i--)
                        {
                            EjectSolid(_chamber.ContainedEntities.ElementAt(i).Uid);
                        }
                    }
                    break;

                case ReagentGrinderEjectChamberContentMessage msg:
                    if (!ChamberEmpty)
                    {
                        EjectSolid(msg.EntityID);
                        ClickSound();
                        _uiDirty = true;
                    }
                    break;

                case ReagentGrinderEjectBeakerMessage msg:
                    ClickSound();
                    EjectBeaker(message.Session.AttachedEntity);
                    //EjectBeaker will dirty the UI for us, we don't have to do it explicitly here.
                    break;
            }
        }

        private void OnPowerStateChanged(PowerChangedMessage e)
        {
            _uiDirty = true;
        }

        private void ClickSound()
        {
            _audioSystem.PlayFromEntity("/Audio/Machines/machine_switch.ogg", Owner, AudioParams.Default.WithVolume(-2f));
        }

        private void SetAppearance()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(ReagentGrinderVisualState.BeakerAttached, HasBeaker);
            }
        }

        public void OnUpdate()
        {
            if(_uiDirty)
            {
                UpdateInterface();
                _uiDirty = false;
            }
        }

        // This doesn't check for UI dirtiness so handle that when calling this.
        private void UpdateInterface()
        {
            bool canJuice = false;
            bool canGrind = false;
            if (HasBeaker)
            {
                foreach (var entity in _chamber.ContainedEntities)
                {
                    if (!canJuice && entity.HasComponent<JuiceableComponent>()) canJuice = true;
                    if (!canGrind && entity.HasTag("Grindable")) canGrind = true;
                    if (canJuice && canGrind) break;
                }
            }

            UserInterface?.SetState(new ReagentGrinderInterfaceState
            (
                _busy,
                HasBeaker,
                Powered,
                canJuice,
                canGrind,
                _chamber.ContainedEntities.Select(item => item.Uid).ToArray(),
                //Remember the beaker can be null!
                _heldBeaker?.Solution.Contents.ToArray()
            ));
            _uiDirty = false;
        }

        private void EjectSolid(EntityUid entityID)
        {
            if (_busy)
                return;

            if (Owner.EntityManager.TryGetEntity(entityID, out var entity))
            {
                _chamber.Remove(entity);

                //Give the ejected entity a tiny bit of offset so each one is apparent in case of a big stack,
                //but (hopefully) not enough to clip it through a solid (wall).
                entity.RandomOffset(0.4f);
            }
            _uiDirty = true;
        }

        /// <summary>
        /// Tries to eject whatever is in the beaker slot. Puts the item in the user's hands or failing that on top
        /// of the grinder.
        /// </summary>
        private void EjectBeaker(IEntity? user)
        {
            if (!HasBeaker || _heldBeaker == null || _busy)
                return;

            _beakerContainer.Remove(_beakerContainer.ContainedEntity);

            if (user == null || !user.TryGetComponent<HandsComponent>(out var hands) || !_heldBeaker.Owner.TryGetComponent<ItemComponent>(out var item))
                return;
            hands.PutInHandOrDrop(item);

            _heldBeaker = null;
            _uiDirty = true;
            SetAppearance();
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }
            _uiDirty = true;
            UserInterface?.Toggle(actor.playerSession);
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IHandsComponent? hands))
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("You have no hands."));
                return true;
            }

            IEntity heldEnt = eventArgs.Using;

            //First, check if user is trying to insert a beaker.
            //No promise it will be a beaker right now, but whatever.
            //Maybe this should whitelist "beaker" in the prototype id of heldEnt?
            if(heldEnt.TryGetComponent(out SolutionContainerComponent? beaker) && beaker.Capabilities.HasFlag(SolutionContainerCaps.FitsInDispenser))
            {
                _beakerContainer.Insert(heldEnt);
                _heldBeaker = beaker;
                _uiDirty = true;
                //We are done, return. Insert the beaker and exit!
                SetAppearance();
                ClickSound();
                return true;
            }

            //Next, see if the user is trying to insert something they want to be ground/juiced.
            if(!heldEnt.HasTag("Grindable") && !heldEnt.TryGetComponent(out JuiceableComponent? juice))
            {
                //Entity did NOT pass the whitelist for grind/juice.
                //Wouldn't want the clown grinding up the Captain's ID card now would you?
                //Why am I asking you? You're biased.
                return false;
            }

            //Cap the chamber. Don't want someone putting in 500 entities and ejecting them all at once.
            //Maybe I should have done that for the microwave too?
            if (_chamber.ContainedEntities.Count >= _storageCap)
            {
                return false;
            }

            if (!_chamber.Insert(heldEnt))
                return false;

            _uiDirty = true;
            return true;
        }

        /// <summary>
        /// The wzhzhzh of the grinder. Processes the contents of the grinder and puts the output in the beaker.
        /// </summary>
        /// <param name="isJuiceIntent">true for wanting to juice, false for wanting to grind.</param>
        private async void DoWork(IEntity user, GrinderProgram program)
        {
            //Have power, are  we busy, chamber has anything to grind, a beaker for the grounds to go?
            if(!Powered || _busy || ChamberEmpty || !HasBeaker || _heldBeaker == null)
            {
                return;
            }

            _busy = true;

            UserInterface?.SendMessage(new ReagentGrinderWorkStartedMessage(program));
            switch (program)
            {
                case GrinderProgram.Grind:
                    _audioSystem.PlayFromEntity("/Audio/Machines/blender.ogg", Owner, AudioParams.Default);
                    //Get each item inside the chamber and get the reagents it contains. Transfer those reagents to the beaker, given we have one in.
                    Owner.SpawnTimer(_workTime, (Action) (() =>
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

                        _busy = false;
                        _uiDirty = true;
                        UserInterface?.SendMessage(new ReagentGrinderWorkCompleteMessage());
                    }));
                    break;

                case GrinderProgram.Juice:
                    _audioSystem.PlayFromEntity("/Audio/Machines/juicer.ogg", Owner, AudioParams.Default);
                    Owner.SpawnTimer(_workTime, (Action) (() =>
                    {
                        foreach (var item in _chamber.ContainedEntities.ToList())
                        {
                            if (!item.TryGetComponent<JuiceableComponent>(out var juiceMe)) continue;
                            if (_heldBeaker.CurrentVolume + juiceMe.JuiceResultSolution.TotalVolume > _heldBeaker.MaxVolume) continue;
                            _heldBeaker.TryAddSolution(juiceMe.JuiceResultSolution);
                            item.Delete();
                        }
                        UserInterface?.SendMessage(new ReagentGrinderWorkCompleteMessage());
                        _busy = false;
                        _uiDirty = true;
                    }));
                    break;
            }
        }
    }
}
