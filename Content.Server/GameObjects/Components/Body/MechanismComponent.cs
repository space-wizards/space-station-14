#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Body.Surgery;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Body
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedMechanismComponent))]
    [ComponentReference(typeof(IMechanism))]
    public class MechanismComponent : SharedMechanismComponent, IAfterInteract
    {
        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(SurgeryUIKey.Key);

        public override void Initialize()
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

            if (eventArgs.Target.TryGetComponent(out IBody? body))
            {
                SendBodyPartListToUser(eventArgs, body);
            }
            else if (eventArgs.Target.TryGetComponent<IBodyPart>(out var part))
            {
                DebugTools.AssertNotNull(part);

                if (!part.TryAddMechanism(this))
                {
                    eventArgs.Target.PopupMessage(eventArgs.User, Loc.GetString("You can't fit it in!"));
                }
            }

            return true;
        }

        private void SendBodyPartListToUser(AfterInteractEventArgs eventArgs, IBody body)
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
                eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                OpenSurgeryUI(actor.playerSession);
                UpdateSurgeryUIBodyPartRequest(actor.playerSession, toSend);
                PerformerCache = eventArgs.User;
                BodyCache = body;
            }
            else // If surgery cannot be performed, show message saying so.
            {
                eventArgs.Target?.PopupMessage(eventArgs.User,
                    Loc.GetString("You see no way to install the {0}.", Owner.Name));
            }
        }

        /// <summary>
        ///     Called after the client chooses from a list of possible BodyParts that can be operated on.
        /// </summary>
        private void HandleReceiveBodyPart(int key)
        {
            if (PerformerCache == null ||
                !PerformerCache.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            CloseSurgeryUI(actor.playerSession);

            if (BodyCache == null)
            {
                return;
            }

            // TODO: sanity checks to see whether user is in range, user is still able-bodied, target is still the same, etc etc
            if (!OptionsCache.TryGetValue(key, out var targetObject))
            {
                BodyCache.Owner.PopupMessage(PerformerCache,
                    Loc.GetString("You see no useful way to use the {0} anymore.", Owner.Name));
                return;
            }

            var target = (IBodyPart) targetObject;
            var message = target.TryAddMechanism(this)
                ? Loc.GetString("You jam {0:theName} inside {1:them}.", Owner, PerformerCache)
                : Loc.GetString("You can't fit it in!");

            BodyCache.Owner.PopupMessage(PerformerCache, message);

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
