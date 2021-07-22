using System;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Chemistry.Components;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Chemistry.Solution;
using Content.Shared.Interaction;
using Content.Shared.Kitchen.Components;
using Content.Shared.Notification.Managers;
using Content.Shared.Random.Helpers;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Kitchen.Components
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
        [ViewVariables] public ContainerSlot BeakerContainer = default!;

        /// <summary>
        /// Can be null since we won't always have a beaker in the grinder.
        /// </summary>
        [ViewVariables] public SolutionContainerComponent? HeldBeaker = default!;

        /// <summary>
        /// Contains the things that are going to be ground or juiced.
        /// </summary>
        [ViewVariables] public Container Chamber = default!;

        /// <summary>
        /// Is the machine actively doing something and can't be used right now?
        /// </summary>
        public bool Busy;

        //YAML serialization vars
        [ViewVariables(VVAccess.ReadWrite)] [DataField("chamberCapacity")] private int _storageCap = 16;
        [ViewVariables(VVAccess.ReadWrite)] [DataField("workTime")] private int _workTime = 3500; //3.5 seconds, completely arbitrary for now.

        private void SetAppearance()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(ReagentGrinderVisualState.BeakerAttached, BeakerContainer.ContainedEntity != null);
            }
        }

        /// <summary>
        /// Tries to eject whatever is in the beaker slot. Puts the item in the user's hands or failing that on top
        /// of the grinder.
        /// </summary>
        private void EjectBeaker(IEntity? user)
        {
            if (!BeakerContainer.ContainedEntity != null || _heldBeaker == null || _busy)
                return;

            var beaker = _beakerContainer.ContainedEntity;
            if(beaker is null)
                return;

            _beakerContainer.Remove(beaker);

            if (user == null || !user.TryGetComponent<HandsComponent>(out var hands) || !_heldBeaker.Owner.TryGetComponent<ItemComponent>(out var item))
                return;
            hands.PutInHandOrDrop(item);

            _heldBeaker = null;
            _uiDirty = true;
            SetAppearance();
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out ActorComponent? actor))
            {
                return;
            }
            _uiDirty = true;
            UserInterface?.Toggle(actor.PlayerSession);
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IHandsComponent? hands))
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("reagent-grinder-component-interact-using-no-hands"));
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
    }
}
