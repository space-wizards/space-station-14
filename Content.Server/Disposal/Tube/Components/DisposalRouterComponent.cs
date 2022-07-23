using System.Text;
using Content.Server.Disposal.Unit.Components;
using Content.Server.UserInterface;
using Content.Shared.Sound;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using static Content.Shared.Disposal.Components.SharedDisposalRouterComponent;

namespace Content.Server.Disposal.Tube.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IDisposalTubeComponent))]
    [ComponentReference(typeof(DisposalTubeComponent))]
    public sealed class DisposalRouterComponent : DisposalJunctionComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        [ViewVariables]
        private readonly HashSet<string> _tags = new();

        [ViewVariables]
        public bool Anchored =>
            !_entMan.TryGetComponent(Owner, out IPhysBody? physics) ||
            physics.BodyType == BodyType.Static;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(DisposalRouterUiKey.Key);

        [DataField("clickSound")] private SoundSpecifier _clickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        public override Direction NextDirection(DisposalHolderComponent holder)
        {
            var directions = ConnectableDirections();

            if (holder.Tags.Overlaps(_tags))
            {
                return directions[1];
            }

            return _entMan.GetComponent<TransformComponent>(Owner).LocalRotation.GetDir();
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

            if (!Anchored)
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
        /// Gets component data to be used to update the user interface client-side.
        /// </summary>
        /// <returns>Returns a <see cref="DisposalRouterUserInterfaceState"/></returns>
        private DisposalRouterUserInterfaceState GetUserInterfaceState()
        {
            if (_tags.Count <= 0)
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
            SoundSystem.Play(_clickSound.GetSound(), Filter.Pvs(Owner), Owner, AudioParams.Default.WithVolume(-2f));
        }

        protected override void OnRemove()
        {
            UserInterface?.CloseAll();
            base.OnRemove();
        }

        public void OpenUserInterface(ActorComponent actor)
        {
            UpdateUserInterface();
            UserInterface?.Open(actor.PlayerSession);
        }
    }
}
