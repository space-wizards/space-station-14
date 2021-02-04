#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Commands;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Body.Surgery;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Body.Part
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedBodyPartComponent))]
    [ComponentReference(typeof(IBodyPart))]
    public class BodyPartComponent : SharedBodyPartComponent, IAfterInteract
    {
        private readonly Dictionary<int, object> _optionsCache = new();
        private IBody? _owningBodyCache;
        private int _idHash;
        private IEntity? _surgeonCache;
        private Container _mechanismContainer = default!;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(SurgeryUIKey.Key);

        public override bool CanAddMechanism(IMechanism mechanism)
        {
            return base.CanAddMechanism(mechanism) &&
                   _mechanismContainer.CanInsert(mechanism.Owner);
        }

        protected override void OnAddMechanism(IMechanism mechanism)
        {
            base.OnAddMechanism(mechanism);

            _mechanismContainer.Insert(mechanism.Owner);
        }

        protected override void OnRemoveMechanism(IMechanism mechanism)
        {
            base.OnRemoveMechanism(mechanism);

            _mechanismContainer.Remove(mechanism.Owner);
            mechanism.Owner.RandomOffset(0.25f);
        }

        public override void Initialize()
        {
            base.Initialize();

            _mechanismContainer = ContainerManagerComponent.Ensure<Container>($"{Name}-{nameof(BodyPartComponent)}", Owner);

            // This is ran in Startup as entities spawned in Initialize
            // are not synced to the client since they are assumed to be
            // identical on it
            foreach (var mechanismId in MechanismIds)
            {
                var entity = Owner.EntityManager.SpawnEntity(mechanismId, Owner.Transform.MapPosition);

                if (!entity.TryGetComponent(out IMechanism? mechanism))
                {
                    Logger.Error($"Entity {mechanismId} does not have a {nameof(IMechanism)} component.");
                    continue;
                }

                TryAddMechanism(mechanism, true);
            }
        }

        protected override void Startup()
        {
            base.Startup();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUIMessage;
            }

            foreach (var mechanism in Mechanisms)
            {
                mechanism.Dirty();
            }
        }

        public async Task<bool> AfterInteract(AfterInteractEventArgs eventArgs)
        {
            // TODO BODY
            if (eventArgs.Target == null)
            {
                return false;
            }

            CloseAllSurgeryUIs();
            _optionsCache.Clear();
            _surgeonCache = null;
            _owningBodyCache = null;

            if (eventArgs.Target.TryGetComponent(out IBody? body))
            {
                SendSlots(eventArgs, body);
            }

            return true;
        }

        private void SendSlots(AfterInteractEventArgs eventArgs, IBody body)
        {
            // Create dictionary to send to client (text to be shown : data sent back if selected)
            var toSend = new Dictionary<string, int>();

            // Here we are trying to grab a list of all empty BodySlots adjacent to an existing BodyPart that can be
            // attached to. i.e. an empty left hand slot, connected to an occupied left arm slot would be valid.
            var unoccupiedSlots = body.Slots.Keys.ToList().Except(body.Parts.Keys.ToList()).ToList();
            foreach (var slot in unoccupiedSlots)
            {
                if (!body.TryGetSlotType(slot, out var typeResult) ||
                    typeResult != PartType ||
                    !body.TryGetPartConnections(slot, out var parts))
                {
                    continue;
                }

                foreach (var connectedPart in parts)
                {
                    if (!connectedPart.CanAttachPart(this))
                    {
                        continue;
                    }

                    _optionsCache.Add(_idHash, slot);
                    toSend.Add(slot, _idHash++);
                }
            }

            if (_optionsCache.Count > 0)
            {
                OpenSurgeryUI(eventArgs.User.GetComponent<BasicActorComponent>().playerSession);
                BodyPartSlotRequest(eventArgs.User.GetComponent<BasicActorComponent>().playerSession,
                    toSend);
                _surgeonCache = eventArgs.User;
                _owningBodyCache = body;
            }
            else // If surgery cannot be performed, show message saying so.
            {
                eventArgs.Target.PopupMessage(eventArgs.User,
                    Loc.GetString("You see no way to install {0:theName}.", Owner));
            }
        }

        /// <summary>
        ///     Called after the client chooses from a list of possible
        ///     BodyPartSlots to install the limb on.
        /// </summary>
        private void ReceiveBodyPartSlot(int key)
        {
            if (_surgeonCache == null ||
                !_surgeonCache.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            CloseSurgeryUI(actor.playerSession);

            if (_owningBodyCache == null)
            {
                return;
            }

            // TODO: sanity checks to see whether user is in range, user is still able-bodied, target is still the same, etc etc
            if (!_optionsCache.TryGetValue(key, out var targetObject))
            {
                _owningBodyCache.Owner.PopupMessage(_surgeonCache,
                    Loc.GetString("You see no useful way to attach {0:theName} anymore.", Owner));
            }

            var target = (string) targetObject!;
            var message = _owningBodyCache.TryAddPart(target, this)
                ? Loc.GetString("You attach {0:theName}.", Owner)
                : Loc.GetString("You can't attach {0:theName}!", Owner);

            _owningBodyCache.Owner.PopupMessage(_surgeonCache, message);
        }

        private void OpenSurgeryUI(IPlayerSession session)
        {
            UserInterface?.Open(session);
        }

        private void BodyPartSlotRequest(IPlayerSession session, Dictionary<string, int> options)
        {
            UserInterface?.SendMessage(new RequestBodyPartSlotSurgeryUIMessage(options), session);
        }

        private void CloseSurgeryUI(IPlayerSession session)
        {
            UserInterface?.Close(session);
        }

        private void CloseAllSurgeryUIs()
        {
            UserInterface?.CloseAll();
        }

        private void OnUIMessage(ServerBoundUserInterfaceMessage message)
        {
            switch (message.Message)
            {
                case ReceiveBodyPartSlotSurgeryUIMessage msg:
                    ReceiveBodyPartSlot(msg.SelectedOptionId);
                    break;
            }
        }

        [Verb]
        public class AttachBodyPartVerb : Verb<BodyPartComponent>
        {
            protected override void GetData(IEntity user, BodyPartComponent component, VerbData data)
            {
                data.Visibility = VerbVisibility.Invisible;

                if (user == component.Owner)
                {
                    return;
                }

                if (!user.TryGetComponent(out IActorComponent? actor))
                {
                    return;
                }

                var groupController = IoCManager.Resolve<IConGroupController>();

                if (!groupController.CanCommand(actor.playerSession, "attachbodypart"))
                {
                    return;
                }

                if (!user.TryGetComponent(out IBody? body))
                {
                    return;
                }

                if (body.HasPart(component))
                {
                    return;
                }

                data.Visibility = VerbVisibility.Visible;
                data.Text = Loc.GetString("Attach Body Part");
            }

            protected override void Activate(IEntity user, BodyPartComponent component)
            {
                if (!user.TryGetComponent(out IBody? body))
                {
                    return;
                }

                body.TryAddPart($"{nameof(AttachBodyPartVerb)}-{component.Owner.Uid}", component, true);
            }
        }
    }
}
