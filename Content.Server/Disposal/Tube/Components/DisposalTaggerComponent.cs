using Content.Server.Disposal.Unit.Components;
using Content.Server.Hands.Components;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using static Content.Shared.Disposal.Components.SharedDisposalTaggerComponent;

namespace Content.Server.Disposal.Tube.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IDisposalTubeComponent))]
    public class DisposalTaggerComponent : DisposalTransitComponent, IActivate
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        [ViewVariables(VVAccess.ReadWrite)]
        private string _tag = "";

        [ViewVariables]
        public bool Anchored =>
            !_entMan.TryGetComponent(Owner, out PhysicsComponent? physics) ||
            physics.BodyType == BodyType.Static;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(DisposalTaggerUiKey.Key);

        [DataField("clickSound")] private SoundSpecifier _clickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        public override Direction NextDirection(DisposalHolderComponent holder)
        {
            holder.Tags.Add(_tag);
            return base.NextDirection(holder);
        }

        protected override void Initialize()
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
            if (session.AttachedEntity is not {} attached)
                return false;
            if (!Anchored)
                return false;

            var actionBlocker = EntitySystem.Get<ActionBlockerSystem>();
            var groupController = IoCManager.Resolve<IConGroupController>();
            //Check if player can interact in their current state
            if (!groupController.CanAdminMenu(session) && (!actionBlocker.CanInteract(attached) || !actionBlocker.CanUse(attached)))
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
            SoundSystem.Play(Filter.Pvs(Owner), _clickSound.GetSound(), Owner, AudioParams.Default.WithVolume(-2f));
        }

        /// <summary>
        /// Called when you click the owner entity with an empty hand. Opens the UI client-side if possible.
        /// </summary>
        /// <param name="args">Data relevant to the event such as the actor which triggered it.</param>
        void IActivate.Activate(ActivateEventArgs args)
        {
            if (!_entMan.TryGetComponent(args.User, out ActorComponent? actor))
            {
                return;
            }

            if (!_entMan.TryGetComponent(args.User, out HandsComponent? hands))
            {
                Owner.PopupMessage(args.User, Loc.GetString("disposal-tagger-window-activate-no-hands"));
                return;
            }

            var activeHandEntity = hands.GetActiveHandItem?.Owner;
            if (activeHandEntity == null)
            {
                OpenUserInterface(actor);
            }
        }

        protected override void OnRemove()
        {
            base.OnRemove();
            UserInterface?.CloseAll();
        }
        public void OpenUserInterface(ActorComponent actor)
        {
            UpdateUserInterface();
            UserInterface?.Open(actor.PlayerSession);
        }
    }
}
