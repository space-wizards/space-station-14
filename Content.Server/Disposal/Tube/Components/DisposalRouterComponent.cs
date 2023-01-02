using Content.Server.Disposal.Unit.Components;
using Content.Server.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using static Content.Shared.Disposal.Components.SharedDisposalRouterComponent;

namespace Content.Server.Disposal.Tube.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IDisposalTubeComponent))]
    [ComponentReference(typeof(DisposalTubeComponent))]
    public sealed class DisposalRouterComponent : DisposalJunctionComponent
    {
        public override string ContainerId => "DisposalRouter";

        [Dependency] private readonly IEntityManager _entMan = default!;

        [DataField("tags")]
        public HashSet<string> Tags = new();

        [ViewVariables]
        public bool Anchored =>
            !_entMan.TryGetComponent(Owner, out PhysicsComponent? physics) ||
            physics.BodyType == BodyType.Static;

        [ViewVariables] public BoundUserInterface? UserInterface => Owner.GetUIOrNull(DisposalRouterUiKey.Key);

        [DataField("clickSound")] private SoundSpecifier _clickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        public override Direction NextDirection(DisposalHolderComponent holder)
        {
            var directions = ConnectableDirections();

            if (holder.Tags.Overlaps(Tags))
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
                Tags.Clear();
                foreach (var tag in msg.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    Tags.Add(tag.Trim());
                    ClickSound();
                }
            }
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
    }
}
