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
using Content.Shared.GameObjects.Components.Power;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Kitchen;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Timers;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Kitchen
{

    /// <summary>
    /// The combo reagent grinder/juicer. The reason why grinding and juicing are seperate is simple,
    /// think of grinding as a utility to break an object down into its reagents. Think of juicing as
    /// converting something into it's single juice form. E.g, grind an apple and get the nutriment and sugar
    /// it contained, juice an apple and get "apple juice".
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class ReagentGrinderComponent : SharedReagentGrinderComponent, IActivate, IInteractUsing
    {
        [Dependency] private readonly IRobustRandom _random = default!;
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
        [ViewVariables] private int _storageCap = 16;
        [ViewVariables] private int _workTime = 3500; //3.5 seconds, completely arbitrary for now.

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

            if (Owner.TryGetComponent(out PowerReceiverComponent? receiver))
            {
                receiver.OnPowerStateChanged += OnPowerStateChanged;
            }

            _audioSystem = EntitySystem.Get<AudioSystem>();
        }

        public override void OnRemove()
        {
            base.OnRemove();
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage -= UserInterfaceOnReceiveMessage;
            }

            if (Owner.TryGetComponent(out PowerReceiverComponent? receiver))
            {
                receiver.OnPowerStateChanged -= OnPowerStateChanged;
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
                    DoWork(message.Session.AttachedEntity!, false);
                    break;

                case ReagentGrinderJuiceStartMessage msg:
                    if (!Powered) break;
                    ClickSound();
                    DoWork(message.Session.AttachedEntity!, true);
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
                    EjectBeaker(message.Session.AttachedEntity!);
                    //EjectBeaker will dirty the UI for us, we don't have to do it explicitly here.
                    break;
            }
        }

        private void OnPowerStateChanged(object? sender, PowerStateEventArgs e)
        {
            _uiDirty = true;
        }

        private void ClickSound()
        {
            _audioSystem.PlayFromEntity("/Audio/Machines/machine_switch.ogg", Owner, AudioParams.Default.WithVolume(-2f));
        }

        private void SetAppearance(ReagentGrinderVisualState state)
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(PowerDeviceVisuals.VisualState, state);
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
                    if (!canGrind && entity.HasComponent<GrindableComponent>()) canGrind = true;
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

            if (Owner.EntityManager.EntityExists(entityID))
            {
                var entity = Owner.EntityManager.GetEntity(entityID);
                _chamber.Remove(entity);

                //Give the ejected entity a tiny bit of offset so each one is apparent in case of a big stack,
                //but (hopefully) not enough to clip it through a solid (wall).
                const float ejectOffset = 0.4f;
                entity.Transform.LocalPosition += (_random.NextFloat() * ejectOffset, _random.NextFloat() * ejectOffset);
            }
            _uiDirty = true;
        }

        /// <summary>
        /// If this component contains an entity with a <see cref="SolutionContainerComponent"/>, eject it.
        /// Tries to eject into user's hands first, then ejects onto dispenser if both hands are full.
        /// </summary>
        private void EjectBeaker(IEntity user)
        {
            if (!HasBeaker || _busy)
                return;

            //Eject the beaker into the hands of the user.
            _beakerContainer.Remove(_beakerContainer.ContainedEntity);

            //UpdateUserInterface();

            if (!user.TryGetComponent<HandsComponent>(out var hands) || !_heldBeaker!.Owner.TryGetComponent<ItemComponent>(out var item))
                return;
            if (hands.CanPutInHand(item))
                hands.PutInHand(item);

            _heldBeaker = null;
            _uiDirty = true;
            SetAppearance(ReagentGrinderVisualState.NoBeaker);
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

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IHandsComponent? hands))
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("You have no hands."));
                return true;
            }

            var heldEnt = eventArgs.Using;

            //First, check if user is trying to insert a beaker.
            //No promise it will be a beaker right now, but whatever.
            //Maybe this should whitelist "beaker" in the prototype id of heldEnt?
            if(heldEnt!.TryGetComponent(out SolutionContainerComponent? beaker) && beaker.Capabilities.HasFlag(SolutionContainerCaps.FitsInDispenser))
            {
                _beakerContainer.Insert(heldEnt);
                _heldBeaker = beaker;
                _uiDirty = true;
                //We are done, return. Insert the beaker and exit!
                SetAppearance(ReagentGrinderVisualState.BeakerAttached);
                ClickSound();
                return true;
            }

            //Next, see if the user is trying to insert something they want to be ground/juiced.
            if(!heldEnt!.TryGetComponent(out GrindableComponent? grind) && !heldEnt!.TryGetComponent(out JuiceableComponent? juice))
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
            _chamber.Insert(heldEnt);
            _uiDirty = true;

            return true;
        }

        /// <summary>
        /// The wzhzhzh of the grinder.
        /// </summary>
        /// <param name="isJuiceIntent">true for wanting to juice, false for wanting to grind.</param>
        private async void DoWork(IEntity user, bool isJuiceIntent)
        {
            //Have power, are  we busy, chamber has anything to grind, a beaker for the grounds to go?
            if(!Powered || _busy || ChamberEmpty || !HasBeaker)
            {
                return;
            }

            _busy = true;

            var chamberContentsArray = _chamber.ContainedEntities.ToArray();
            var chamberCount = chamberContentsArray.Length;

            //This block is for grinding behaviour only.
            if (!isJuiceIntent)
            {
                UserInterface?.SendMessage(new ReagentGrinderWorkStartedMessage(isJuiceIntent));
                _audioSystem.PlayFromEntity("/Audio/Machines/blender.ogg", Owner, AudioParams.Default);
                //Get each item inside the chamber and get the reagents it contains. Transfer those reagents to the beaker, given we have one in.
                Owner.SpawnTimer(_workTime, (Action) (() =>
                {
                    for (int i = chamberCount - 1; i >= 0; i--)
                    {
                        var item = chamberContentsArray[i];
                        if (item.TryGetComponent<SolutionContainerComponent>(out var solution))
                        {
                            if (_heldBeaker!.CurrentVolume + solution.CurrentVolume > _heldBeaker!.MaxVolume) continue;
                            _heldBeaker!.TryAddSolution(solution.Solution);
                            solution!.RemoveAllSolution();
                            _chamber.ContainedEntities[i].Delete();
                        }
                    }

                    _busy = false;
                    _uiDirty = true;
                    UserInterface?.SendMessage(new ReagentGrinderWorkCompleteMessage());
                    return;
                }));
            }
            else
            {
                UserInterface?.SendMessage(new ReagentGrinderWorkStartedMessage(isJuiceIntent));
                _audioSystem.PlayFromEntity("/Audio/Machines/juicer.ogg", Owner, AudioParams.Default);
                Owner.SpawnTimer(_workTime, (Action) (() =>
                {
                    //OK, so if we made it this far we want to juice instead.
                    for (int i = chamberCount - 1; i >= 0; i--)
                    {
                        var item = chamberContentsArray[i];
                        if (item.TryGetComponent<JuiceableComponent>(out var juiceMe))
                        {
                            if (_heldBeaker!.CurrentVolume + juiceMe.JuiceResultSolution.TotalVolume > _heldBeaker!.MaxVolume) continue;
                            _heldBeaker!.TryAddSolution(juiceMe.JuiceResultSolution);
                            _chamber.ContainedEntities[i].Delete();
                        }
                    }
                    UserInterface?.SendMessage(new ReagentGrinderWorkCompleteMessage());
                    _busy = false;
                    _uiDirty = true;
                }));
            }
        }
    }
}
