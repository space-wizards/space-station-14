#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Server.Utility;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;
using static Content.Shared.GameObjects.Components.Disposal.SharedDisposalRouterComponent;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IDisposalTubeComponent))]
    public class DisposalRouterComponent : DisposalJunctionComponent, IActivate
    {
        public override string Name => "DisposalRouter";

        [ViewVariables]
        private readonly HashSet<string> _tags = new HashSet<string>();

        [ViewVariables]
        public bool Anchored =>
            !Owner.TryGetComponent(out ICollidableComponent? collidable) ||
            collidable.Anchored;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(DisposalRouterUiKey.Key);

        public override Direction NextDirection(DisposalHolderComponent holder)
        {
            var directions = ConnectableDirections();

            if (holder.Tags.Overlaps(_tags))
            {
                return directions[1];
            }

            return Owner.Transform.LocalRotation.GetDir();
        }

        public override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }

            UpdateUserInterface();
        }

        /// <summary>
        /// Handles ui messages from the client. For things such as button presses
        /// which interact with the  world and require server action.
        /// </summary>
        /// <param name="obj">A user interface message from the client.</param>
        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (obj.Session.AttachedEntity == null)
            {
                return;
            }

            var msg = (UiActionMessage) obj.Message;

            if (!PlayerCanUseDisposalTagger(obj.Session.AttachedEntity))
                return;

            //Check for correct message and ignore maleformed strings
            if (msg.Action == UiAction.Ok && TagRegex.IsMatch(msg.Tags))
            {
                _tags.Clear();
                foreach (var tag in msg.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    _tags.Add(tag.Trim());
                    ClickSound();
                }
            }
        }

        /// <summary>
        /// Checks whether the player entity is able to use the configuration interface of the pipe tagger.
        /// </summary>
        /// <param name="playerEntity">The player entity.</param>
        /// <returns>Returns true if the entity can use the configuration interface, and false if it cannot.</returns>
        private bool PlayerCanUseDisposalTagger(IEntity playerEntity)
        {
            //Need player entity to check if they are still able to use the configuration interface
            if (playerEntity == null)
                return false;
            if (!Anchored)
                return false;
            //Check if player can interact in their current state
            if (!ActionBlockerSystem.CanInteract(playerEntity) || !ActionBlockerSystem.CanUse(playerEntity))
                return false;

            return true;
        }

        /// <summary>
        /// Gets component data to be used to update the user interface client-side.
        /// </summary>
        /// <returns>Returns a <see cref="DisposalRouterUserInterfaceState"/></returns>
        private DisposalRouterUserInterfaceState GetUserInterfaceState()
        {
            if(_tags.Count <= 0)
            {
                return new DisposalRouterUserInterfaceState("");
            }

            var taglist = new StringBuilder();

            foreach (var tag in _tags)
            {
                taglist.Append(tag);
                taglist.Append(", ");
            }

            taglist.Remove(taglist.Length - 2, 2);

            return new DisposalRouterUserInterfaceState(taglist.ToString());
        }

        private void UpdateUserInterface()
        {
            var state = GetUserInterfaceState();
            UserInterface?.SetState(state);
        }

        private void ClickSound()
        {
            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Machines/machine_switch.ogg", Owner, AudioParams.Default.WithVolume(-2f));
        }

        /// <summary>
        /// Called when you click the owner entity with an empty hand. Opens the UI client-side if possible.
        /// </summary>
        /// <param name="args">Data relevant to the event such as the actor which triggered it.</param>
        void IActivate.Activate(ActivateEventArgs args)
        {
            if (!args.User.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            if (!args.User.TryGetComponent(out IHandsComponent? hands))
            {
                Owner.PopupMessage(args.User, Loc.GetString("You have no hands."));
                return;
            }

            var activeHandEntity = hands.GetActiveHand?.Owner;
            if (activeHandEntity == null)
            {
                UpdateUserInterface();
                UserInterface?.Open(actor.playerSession);
            }
        }

        public override void OnRemove()
        {
            UserInterface?.CloseAll();
            base.OnRemove();
        }
    }
}
