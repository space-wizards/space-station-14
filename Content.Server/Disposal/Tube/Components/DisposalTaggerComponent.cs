using Content.Server.Disposal.Unit.Components;
using Content.Server.UserInterface;
using Content.Shared.Sound;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using static Content.Shared.Disposal.Components.SharedDisposalTaggerComponent;

namespace Content.Server.Disposal.Tube.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IDisposalTubeComponent))]
    [ComponentReference(typeof(DisposalTubeComponent))]
    public sealed class DisposalTaggerComponent : DisposalTransitComponent
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

            if (!Anchored)
                return;

            //Check for correct message and ignore maleformed strings
            if (msg.Action == UiAction.Ok && TagRegex.IsMatch(msg.Tag))
            {
                    _tag = msg.Tag;
                    ClickSound();
            }
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
            SoundSystem.Play(_clickSound.GetSound(), Filter.Pvs(Owner), Owner, AudioParams.Default.WithVolume(-2f));
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
