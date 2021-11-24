using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.UserInterface;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Surgery;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Verbs;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Part
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedBodyPartComponent))]
    public class BodyPartComponent : SharedBodyPartComponent, IAfterInteract
    {
        private readonly Dictionary<int, object> _optionsCache = new();
        private SharedBodyComponent? _owningBodyCache;
        private int _idHash;
        private IEntity? _surgeonCache;
        private Container _mechanismContainer = default!;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(SurgeryUIKey.Key);

        public override bool CanAddMechanism(SharedMechanismComponent mechanism)
        {
            return base.CanAddMechanism(mechanism) &&
                   _mechanismContainer.CanInsert(mechanism.Owner);
        }

        protected override void OnAddMechanism(SharedMechanismComponent mechanism)
        {
            base.OnAddMechanism(mechanism);

            _mechanismContainer.Insert(mechanism.Owner);
        }

        protected override void OnRemoveMechanism(SharedMechanismComponent mechanism)
        {
            base.OnRemoveMechanism(mechanism);

            _mechanismContainer.Remove(mechanism.Owner);
            mechanism.Owner.RandomOffset(0.25f);
        }

        protected override void Initialize()
        {
            base.Initialize();

            _mechanismContainer = Owner.EnsureContainer<Container>($"{Name}-{nameof(BodyPartComponent)}");

            // This is ran in Startup as entities spawned in Initialize
            // are not synced to the client since they are assumed to be
            // identical on it
            foreach (var mechanismId in MechanismIds)
            {
                var entity = Owner.EntityManager.SpawnEntity(mechanismId, Owner.Transform.MapPosition);

                if (!entity.TryGetComponent(out SharedMechanismComponent? mechanism))
                {
                    Logger.Error($"Entity {mechanismId} does not have a {nameof(SharedMechanismComponent)} component.");
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

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
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

            if (eventArgs.Target.TryGetComponent(out SharedBodyComponent? body))
            {
                SendSlots(eventArgs, body);
            }

            return true;
        }

        private void SendSlots(AfterInteractEventArgs eventArgs, SharedBodyComponent body)
        {
            // Create dictionary to send to client (text to be shown : data sent back if selected)
            var toSend = new Dictionary<string, int>();

            // Here we are trying to grab a list of all empty BodySlots adjacent to an existing BodyPart that can be
            // attached to. i.e. an empty left hand slot, connected to an occupied left arm slot would be valid.
            foreach (var slot in body.EmptySlots)
            {
                if (slot.PartType != PartType)
                {
                    continue;
                }

                foreach (var connection in slot.Connections)
                {
                    if (connection.Part == null ||
                        !connection.Part.CanAttachPart(this))
                    {
                        continue;
                    }

                    _optionsCache.Add(_idHash, slot);
                    toSend.Add(slot.Id, _idHash++);
                }
            }

            if (_optionsCache.Count > 0)
            {
                OpenSurgeryUI(eventArgs.User.GetComponent<ActorComponent>().PlayerSession);
                BodyPartSlotRequest(eventArgs.User.GetComponent<ActorComponent>().PlayerSession,
                    toSend);
                _surgeonCache = eventArgs.User;
                _owningBodyCache = body;
            }
            else // If surgery cannot be performed, show message saying so.
            {
                eventArgs.Target?.PopupMessage(eventArgs.User,
                    Loc.GetString("bodypart-component-no-way-to-install-message", ("partName", Owner)));
            }
        }

        /// <summary>
        ///     Called after the client chooses from a list of possible
        ///     BodyPartSlots to install the limb on.
        /// </summary>
        private void ReceiveBodyPartSlot(int key)
        {
            if (_surgeonCache == null ||
                !_surgeonCache.TryGetComponent(out ActorComponent? actor))
            {
                return;
            }

            CloseSurgeryUI(actor.PlayerSession);

            if (_owningBodyCache == null)
            {
                return;
            }

            // TODO: sanity checks to see whether user is in range, user is still able-bodied, target is still the same, etc etc
            if (!_optionsCache.TryGetValue(key, out var targetObject))
            {
                _owningBodyCache.Owner.PopupMessage(_surgeonCache,
                    Loc.GetString("bodypart-component-no-way-to-attach-message", ("partName", Owner)));
            }

            var target = (string) targetObject!;
            var message = _owningBodyCache.TryAddPart(target, this)
                ? Loc.GetString("bodypart-component-attach-success-message",("partName", Owner))
                : Loc.GetString("bodypart-component-attach-fail-message",("partName", Owner));

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
    }
}
