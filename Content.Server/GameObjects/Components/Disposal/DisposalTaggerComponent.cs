#nullable enable
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Server.Utility;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.Console;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using static Content.Shared.GameObjects.Components.Disposal.SharedDisposalTaggerComponent;

namespace Content.Server.GameObjects.Components.Disposal
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IDisposalTubeComponent))]
    public class DisposalTaggerComponent : DisposalTransitComponent, IActivate
    {
        public override string Name => "DisposalTagger";

        [ViewVariables(VVAccess.ReadWrite)]
        private string _tag = "";

        [ViewVariables]
        public bool Anchored =>
            !Owner.TryGetComponent(out PhysicsComponent? physics) ||
            physics.Anchored;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(DisposalTaggerUiKey.Key);

        public override Direction NextDirection(DisposalHolderComponent holder)
        {
            holder.Tags.Add(_tag);
            return base.NextDirection(holder);
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
        /// which interact with the world and require server action.
        /// </summary>
        /// <param name="obj">A user interface message from the client.</param>
        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            var msg = (UiActionMessage) obj.Message;

            if (!PlayerCanUseDisposalTagger(obj.Session))
                return;

            //Check for correct message and ignore maleformed strings
            if (msg.Action == UiAction.Ok && TagRegex.IsMatch(msg.Tag))
            {
                    _tag = msg.Tag;
                    ClickSound();
            }
        }

        /// <summary>
        /// Checks whether the player entity is able to use the configuration interface of the pipe tagger.
        /// </summary>
        /// <param name="IPlayerSession">The player entity.</param>
        /// <returns>Returns true if the entity can use the configuration interface, and false if it cannot.</returns>
        private bool PlayerCanUseDisposalTagger(IPlayerSession session)
        {
            //Need player entity to check if they are still able to use the configuration interface
            if (session.AttachedEntity == null)
                return false;
            if (!Anchored)
                return false;

            var groupController = IoCManager.Resolve<IConGroupController>();
            //Check if player can interact in their current state
            if (!groupController.CanAdminMenu(session) && (!ActionBlockerSystem.CanInteract(session.AttachedEntity) || !ActionBlockerSystem.CanUse(session.AttachedEntity)))
                return false;

            return true;
        }

        /// <summary>
        /// Gets component data to be used to update the user interface client-side.
        /// </summary>
        /// <returns>Returns a <see cref="DisposalTaggerUserInterfaceState"/></returns>
        private DisposalTaggerUserInterfaceState GetUserInterfaceState()
        {
            return new(_tag);
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
                OpenUserInterface(actor);
            }
        }

        public override void OnRemove()
        {
            base.OnRemove();
            UserInterface?.CloseAll();
        }

        [Verb]
        public sealed class ConfigureVerb : Verb<DisposalTaggerComponent>
        {
            protected override void GetData(IEntity user, DisposalTaggerComponent component, VerbData data)
            {

                var groupController = IoCManager.Resolve<IConGroupController>();
                if (!user.TryGetComponent(out IActorComponent? actor) || !groupController.CanAdminMenu(actor.playerSession))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Open Configuration");
                data.IconTexture = "/Textures/Interface/VerbIcons/settings.svg.96dpi.png";
            }

            protected override void Activate(IEntity user, DisposalTaggerComponent component)
            {
                if (user.TryGetComponent(out IActorComponent? actor))
                {
                    component.OpenUserInterface(actor);
                }
            }
        }

        private void OpenUserInterface(IActorComponent actor)
        {
            UpdateUserInterface();
            UserInterface?.Open(actor.playerSession);
        }
    }
}
