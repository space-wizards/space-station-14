using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Body.Components;
using Content.Server.Body.Surgery.Messages;
using Content.Server.UserInterface;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Surgery;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Surgery.Components
{
    /// <summary>
    ///     Server-side component representing a generic tool capable of performing surgery.
    ///     For instance, the scalpel.
    /// </summary>
    [RegisterComponent]
    public class SurgeryToolComponent : Component, ISurgeon, IAfterInteract
    {
        public override string Name => "SurgeryTool";

        private readonly Dictionary<int, object> _optionsCache = new();

        [DataField("baseOperateTime")]
        private float _baseOperateTime = 5;

        private ISurgeon.MechanismRequestCallback? _callbackCache;

        private int _idHash;

        [DataField("surgeryType")]
        private SurgeryType _surgeryType = SurgeryType.Incision;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(SurgeryUIKey.Key);

        public SharedBodyComponent? BodyCache { get; private set; }

        public IEntity? PerformerCache { get; private set; }

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null)
            {
                return false;
            }

            if (!eventArgs.User.TryGetComponent(out ActorComponent? actor))
            {
                return false;
            }

            CloseAllSurgeryUIs();

            // Attempt surgery on a body by sending a list of operable parts for the client to choose from
            if (eventArgs.Target.TryGetComponent(out SharedBodyComponent? body))
            {
                // Create dictionary to send to client (text to be shown : data sent back if selected)
                var toSend = new Dictionary<string, int>();

                foreach (var (part, slot) in body.Parts)
                {
                    // For each limb in the target, add it to our cache if it is a valid option.
                    if (part.SurgeryCheck(_surgeryType))
                    {
                        _optionsCache.Add(_idHash, part);
                        toSend.Add(slot.Id + ": " + part.Name, _idHash++);
                    }
                }

                if (_optionsCache.Count > 0)
                {
                    OpenSurgeryUI(actor.PlayerSession);
                    UpdateSurgeryUIBodyPartRequest(actor.PlayerSession, toSend);
                    PerformerCache = eventArgs.User; // Also, cache the data.
                    BodyCache = body;
                }
                else // If surgery cannot be performed, show message saying so.
                {
                    NotUsefulPopup();
                }
            }
            else if (eventArgs.Target.TryGetComponent<SharedBodyPartComponent>(out var part))
            {
                // Attempt surgery on a DroppedBodyPart - there's only one possible target so no need for selection UI
                PerformerCache = eventArgs.User;

                // If surgery can be performed...
                if (!part.SurgeryCheck(_surgeryType))
                {
                    NotUsefulPopup();
                    return true;
                }

                // ...do the surgery.
                if (part.AttemptSurgery(_surgeryType, part, this,
                    eventArgs.User))
                {
                    return true;
                }

                // Log error if the surgery fails somehow.
                Logger.Debug($"Error when trying to perform surgery on ${nameof(SharedBodyPartComponent)} {eventArgs.User.Name}");
                throw new InvalidOperationException();
            }

            return true;
        }

        public float BaseOperationTime { get => _baseOperateTime; set => _baseOperateTime = value; }

        public void RequestMechanism(IEnumerable<SharedMechanismComponent> options, ISurgeon.MechanismRequestCallback callback)
        {
            var toSend = new Dictionary<string, int>();
            foreach (var mechanism in options)
            {
                _optionsCache.Add(_idHash, mechanism);
                toSend.Add(mechanism.Name, _idHash++);
            }

            if (_optionsCache.Count > 0 && PerformerCache != null)
            {
                OpenSurgeryUI(PerformerCache.GetComponent<ActorComponent>().PlayerSession);
                UpdateSurgeryUIMechanismRequest(PerformerCache.GetComponent<ActorComponent>().PlayerSession,
                    toSend);
                _callbackCache = callback;
            }
            else
            {
                Logger.Debug("Error on callback from mechanisms: there were no viable options to choose from!");
                throw new InvalidOperationException();
            }
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            }
        }

        private void OpenSurgeryUI(IPlayerSession session)
        {
            UserInterface?.Open(session);

            var message = new SurgeryWindowOpenMessage(this);

#pragma warning disable 618
            SendMessage(message);
#pragma warning restore 618
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, message);
        }

        private void UpdateSurgeryUIBodyPartRequest(IPlayerSession session, Dictionary<string, int> options)
        {
            UserInterface?.SendMessage(new RequestBodyPartSurgeryUIMessage(options), session);
        }

        private void UpdateSurgeryUIMechanismRequest(IPlayerSession session, Dictionary<string, int> options)
        {
            UserInterface?.SendMessage(new RequestMechanismSurgeryUIMessage(options), session);
        }

        private void ClearUIData()
        {
            _optionsCache.Clear();

            PerformerCache = null;
            BodyCache = null;
            _callbackCache = null;
        }

        private void CloseSurgeryUI(IPlayerSession session)
        {
            UserInterface?.Close(session);
            ClearUIData();
        }

        public void CloseAllSurgeryUIs()
        {
            UserInterface?.CloseAll();
            ClearUIData();
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage message)
        {
            switch (message.Message)
            {
                case ReceiveBodyPartSurgeryUIMessage msg:
                    HandleReceiveBodyPart(msg.SelectedOptionId);
                    break;
                case ReceiveMechanismSurgeryUIMessage msg:
                    HandleReceiveMechanism(msg.SelectedOptionId);
                    break;
            }
        }

        /// <summary>
        ///     Called after the client chooses from a list of possible
        ///     <see cref="SharedBodyPartComponent"/> that can be operated on.
        /// </summary>
        private void HandleReceiveBodyPart(int key)
        {
            if (PerformerCache == null ||
                !PerformerCache.TryGetComponent(out ActorComponent? actor))
            {
                return;
            }

            CloseSurgeryUI(actor.PlayerSession);
            // TODO: sanity checks to see whether user is in range, user is still able-bodied, target is still the same, etc etc
            if (!_optionsCache.TryGetValue(key, out var targetObject) ||
                BodyCache == null)
            {
                NotUsefulAnymorePopup();
                return;
            }

            var target = (SharedBodyPartComponent) targetObject!;

            // TODO BODY Reconsider
            if (!target.AttemptSurgery(_surgeryType, BodyCache, this, PerformerCache))
            {
                NotUsefulAnymorePopup();
            }
        }

        /// <summary>
        ///     Called after the client chooses from a list of possible
        ///     <see cref="SharedMechanismComponent"/> to choose from.
        /// </summary>
        private void HandleReceiveMechanism(int key)
        {
            // TODO: sanity checks to see whether user is in range, user is still able-bodied, target is still the same, etc etc
            if (BodyCache == null ||
                !_optionsCache.TryGetValue(key, out var targetObject) ||
                targetObject is not MechanismComponent target ||
                PerformerCache == null ||
                !PerformerCache.TryGetComponent(out ActorComponent? actor))
            {
                NotUsefulAnymorePopup();
                return;
            }

            CloseSurgeryUI(actor.PlayerSession);
            _callbackCache?.Invoke(target, BodyCache, this, PerformerCache);
        }

        private void NotUsefulPopup()
        {
            if (PerformerCache == null) return;

            BodyCache?.Owner.PopupMessage(PerformerCache,
                Loc.GetString("surgery-tool-component-not-useful-message", ("bodyPart", Owner)));
        }

        private void NotUsefulAnymorePopup()
        {
            if (PerformerCache == null) return;

            BodyCache?.Owner.PopupMessage(PerformerCache,
                Loc.GetString("surgery-tool-component-not-useful-anymore-message", ("bodyPart", Owner)));
        }
    }
}
