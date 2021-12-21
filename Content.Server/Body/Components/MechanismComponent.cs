using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.UserInterface;
using Content.Shared.Body.Components;
using Content.Shared.Body.Surgery;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedMechanismComponent))]
    public class MechanismComponent : SharedMechanismComponent, IAfterInteract
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(SurgeryUIKey.Key);

        protected override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUIMessage;
            }
        }

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null)
            {
                return false;
            }

            CloseAllSurgeryUIs();
            OptionsCache.Clear();
            PerformerCache = null;
            BodyCache = null;

            if (_entities.TryGetComponent(eventArgs.Target.Value, out SharedBodyComponent? body))
            {
                SendBodyPartListToUser(eventArgs, body);
            }
            else if (_entities.TryGetComponent<SharedBodyPartComponent?>(eventArgs.Target.Value, out var part))
            {
                DebugTools.AssertNotNull(part);

                if (!part.TryAddMechanism(this))
                {
                    eventArgs.Target.Value.PopupMessage(eventArgs.User, Loc.GetString("mechanism-component-cannot-fit-message"));
                }
            }

            return true;
        }

        private void SendBodyPartListToUser(AfterInteractEventArgs eventArgs, SharedBodyComponent body)
        {
            // Create dictionary to send to client (text to be shown : data sent back if selected)
            var toSend = new Dictionary<string, int>();

            foreach (var (part, slot) in body.Parts)
            {
                // For each limb in the target, add it to our cache if it is a valid option.
                if (part.CanAddMechanism(this))
                {
                    OptionsCache.Add(IdHash, slot);
                    toSend.Add(part + ": " + part.Name, IdHash++);
                }
            }

            if (OptionsCache.Count > 0 &&
                _entities.TryGetComponent(eventArgs.User, out ActorComponent? actor))
            {
                OpenSurgeryUI(actor.PlayerSession);
                UpdateSurgeryUIBodyPartRequest(actor.PlayerSession, toSend);
                PerformerCache = eventArgs.User;
                BodyCache = body;
            }
            else // If surgery cannot be performed, show message saying so.
            {
                eventArgs.Target?.PopupMessage(eventArgs.User,
                    Loc.GetString("mechanism-component-no-way-to-install-message", ("partName", Name: _entities.GetComponent<MetaDataComponent>(Owner).EntityName)));
            }
        }

        /// <summary>
        ///     Called after the client chooses from a list of possible BodyParts that can be operated on.
        /// </summary>
        private void HandleReceiveBodyPart(int key)
        {
            if (PerformerCache == null ||
                !_entities.TryGetComponent(PerformerCache.Value, out ActorComponent? actor))
            {
                return;
            }

            CloseSurgeryUI(actor.PlayerSession);

            if (BodyCache == null)
            {
                return;
            }

            // TODO: sanity checks to see whether user is in range, user is still able-bodied, target is still the same, etc etc
            if (!OptionsCache.TryGetValue(key, out var targetObject))
            {
                BodyCache.Owner.PopupMessage(PerformerCache.Value,
                    Loc.GetString("mechanism-component-no-useful-way-to-use-message",("partName", Name: _entities.GetComponent<MetaDataComponent>(Owner).EntityName)));
                return;
            }

            var target = (SharedBodyPartComponent) targetObject;
            var message = target.TryAddMechanism(this)
                ? Loc.GetString("mechanism-component-jam-inside-message",("ownerName", Owner),("them", PerformerCache))
                : Loc.GetString("mechanism-component-cannot-fit-message");

            BodyCache.Owner.PopupMessage(PerformerCache.Value, message);

            // TODO: {1:theName}
        }

        private void OpenSurgeryUI(IPlayerSession session)
        {
            UserInterface?.Open(session);
        }

        private void UpdateSurgeryUIBodyPartRequest(IPlayerSession session, Dictionary<string, int> options)
        {
            UserInterface?.SendMessage(new RequestBodyPartSurgeryUIMessage(options), session);
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
                case ReceiveBodyPartSurgeryUIMessage msg:
                    HandleReceiveBodyPart(msg.SelectedOptionId);
                    break;
            }
        }
    }
}
