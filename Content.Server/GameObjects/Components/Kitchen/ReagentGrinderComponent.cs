#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Server.Utility;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Kitchen;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Kitchen
{


    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    /// <summary>
    /// The combo reagent grinder/juicer. The reason why grinding and juicing are seperate is simple,
    /// think of grinding as a utility to break an object down into its reagents. Think of juicing as
    /// converting something into it's single juice form. E.g, grind an apple and get the nutriment and sugar
    /// it contained, juice an apple and get "apple juice".
    /// </summary>
    public class ReagentGrinderComponent : SharedReagentGrinderComponent, IActivate, IInteractUsing
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;



        [ViewVariables] private ContainerSlot _beakerContainer = default!;

        /// <summary>
        /// Can be null since we won't always have a beaker in the grinder.
        /// </summary>
        [ViewVariables] private SolutionContainerComponent? _heldBeaker = default!;

        /// <summary>
        /// Contains the things that are going to be ground or juiced.
        /// </summary>
        [ViewVariables] private Container _chamber = default!;
        [ViewVariables] private bool HasBeaker => _beakerContainer.ContainedEntity != null;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(ReagentGrinderUiKey.Key);


        private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent? receiver) || receiver.Powered;

        /// <summary>
        /// Should the BoundUI be told to update?
        /// </summary>
        private bool _dirty = true;
        /// <summary>
        /// Is the machine actively doing something and can't be used right now?
        /// </summary>
        private bool _busy = false;


        //Serialization vars
        private List<string> _grindableIds = new List<string>();
        private int _storageCap = 16;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);    
            serializer.DataField(ref _grindableIds, "grind_list", new List<string>());
            serializer.DataField(ref _storageCap, "solids_capacity", 16);
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

        }

        private void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage message)
        {
            if(!Powered || _busy)
            {
                return;
            }

            switch(message.Message)
            {
                case ReagentGrinderEjectBeakerMessage msg:
                    EjectBeaker(message.Session.AttachedEntity!);
                    //EjectBeaker will dirty the UI for us, we don't have to do it explicitly here.
                    break;
            }
        }

        public void OnUpdate()
        {
            if(_dirty)
            {
                UserInterface?.SetState(new ReagentGrinderInterfaceState
                (
                    HasBeaker,
                    _chamber.ContainedEntities.Select(item => item.Uid).ToArray(),
                    //Remember the beaker can be null!
                    _heldBeaker?.Solution.Contents.ToArray()
                ));
                _dirty = false;
            }
        }

        /// <summary>
        /// If this component contains an entity with a <see cref="SolutionContainerComponent"/>, eject it.
        /// Tries to eject into user's hands first, then ejects onto dispenser if both hands are full.
        /// </summary>
        private void EjectBeaker(IEntity user)
        {
            if (!HasBeaker)
                return;

            //Eject the beaker into the hands of the user.
            _beakerContainer.Remove(_beakerContainer.ContainedEntity);

            //UpdateUserInterface();

            if (!user.TryGetComponent<HandsComponent>(out var hands) || !_heldBeaker!.Owner.TryGetComponent<ItemComponent>(out var item))
                return;
            if (hands.CanPutInHand(item))
                hands.PutInHand(item);

            _heldBeaker = null;
            _dirty = true;
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor) || !Powered)
            {
                return;
            }

            _dirty = true;
            UserInterface?.Toggle(actor.playerSession);
        }

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {

            //look at this dude, got no hands.
            if (!eventArgs.User.TryGetComponent(out IHandsComponent? hands))
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("You have no hands."));
                return true;
            }

            
            var heldEnt = eventArgs.Using;

            //First, check if user is trying to insert a beaker.
            //No promise it will be a beaker right now, but whatever.
            //Maybe this should whitelist "beaker" in the prototype id of heldEnt?
            if(heldEnt!.TryGetComponent(out SolutionContainerComponent? beaker) && heldEnt!.Prototype!.ID.ToLower().Contains("beaker"))
            {
                _beakerContainer.Insert(heldEnt);
                _heldBeaker = beaker;
                _dirty = true;
                return true;
            }

            //Next, see if the user is trying to insert something they want to be ground/juiced.

            /*Magic number of 16 at the moment to cap the chamber, will be a constant or yaml based.
            Don't want someone putting in 500 entities and ejecting them all at once. Maybe I should have done that for the microwave too?
            */
            if (!_grindableIds.Contains(heldEnt!.Prototype!.ID) || _chamber.ContainedEntities.Count >= 16) return false;
            _chamber.Insert(heldEnt);
            _dirty = true;

            return true;
        }


        private void Grind()
        {

        }

        private void Juice()
        {

        }
    }
}
