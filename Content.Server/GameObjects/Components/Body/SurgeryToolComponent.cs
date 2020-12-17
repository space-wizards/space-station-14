#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Utility;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Body.Surgery;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Body
{
    /// <summary>
    ///     Server-side component representing a generic tool capable of performing surgery.
    ///     For instance, the scalpel.
    /// </summary>
    [RegisterComponent]
    public class SurgeryToolComponent : Component, ISurgeon, IAfterInteract
    {
        public override string Name => "SurgeryTool";
        public override uint? NetID => ContentNetIDs.SURGERY;

        private readonly Dictionary<int, object> _optionsCache = new();

        private float _baseOperateTime;

        private IBody? _bodyCache;

        private ISurgeon.MechanismRequestCallback? _callbackCache;

        private int _idHash;

        private IEntity? _performerCache;

        private SurgeryType _surgeryType;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(SurgeryUIKey.Key);

        async Task IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null)
            {
                return;
            }

            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            CloseAllSurgeryUIs();
            _optionsCache.Clear();

            _performerCache = null;
            _bodyCache = null;
            _callbackCache = null;

            // Attempt surgery on a body by sending a list of operable parts for the client to choose from
            if (eventArgs.Target.TryGetComponent(out IBody? body))
            {
                // Create dictionary to send to client (text to be shown : data sent back if selected)
                var toSend = new Dictionary<string, int>();

                foreach (var (key, value) in body.Parts)
                {
                    // For each limb in the target, add it to our cache if it is a valid option.
                    if (value.SurgeryCheck(_surgeryType))
                    {
                        _optionsCache.Add(_idHash, value);
                        toSend.Add(key + ": " + value.Name, _idHash++);
                    }
                }

                if (_optionsCache.Count > 0)
                {
                    OpenSurgeryUI(actor.playerSession);
                    UpdateSurgeryUIBodyPartRequest(actor.playerSession, toSend);
                    _performerCache = eventArgs.User; // Also, cache the data.
                    _bodyCache = body;
                }
                else // If surgery cannot be performed, show message saying so.
                {
                    NotUsefulPopup();
                }
            }
            else if (eventArgs.Target.TryGetComponent<IBodyPart>(out var part))
            {
                // Attempt surgery on a DroppedBodyPart - there's only one possible target so no need for selection UI
                _performerCache = eventArgs.User;

                // If surgery can be performed...
                if (!part.SurgeryCheck(_surgeryType))
                {
                    NotUsefulPopup();
                    return;
                }

                // ...do the surgery.
                if (part.AttemptSurgery(_surgeryType, part, this,
                    eventArgs.User))
                {
                    return;
                }

                // Log error if the surgery fails somehow.
                Logger.Debug($"Error when trying to perform surgery on ${nameof(IBodyPart)} {eventArgs.User.Name}");
                throw new InvalidOperationException();
            }
        }

        public float BaseOperationTime { get => _baseOperateTime; set => _baseOperateTime = value; }

        public void RequestMechanism(IEnumerable<IMechanism> options, ISurgeon.MechanismRequestCallback callback)
        {
            var toSend = new Dictionary<string, int>();
            foreach (var mechanism in options)
            {
                _optionsCache.Add(_idHash, mechanism);
                toSend.Add(mechanism.Name, _idHash++);
            }

            if (_optionsCache.Count > 0 && _performerCache != null)
            {
                OpenSurgeryUI(_performerCache.GetComponent<BasicActorComponent>().playerSession);
                UpdateSurgeryUIMechanismRequest(_performerCache.GetComponent<BasicActorComponent>().playerSession,
                    toSend);
                _callbackCache = callback;
            }
            else
            {
                Logger.Debug("Error on callback from mechanisms: there were no viable options to choose from!");
                throw new InvalidOperationException();
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            }
        }

        // TODO BODY add checks to close UI if user walks too far away from tool or target.
        private void OpenSurgeryUI(IPlayerSession session)
        {
            UserInterface?.Open(session);
        }

        private void UpdateSurgeryUIBodyPartRequest(IPlayerSession session, Dictionary<string, int> options)
        {
            UserInterface?.SendMessage(new RequestBodyPartSurgeryUIMessage(options), session);
        }

        private void UpdateSurgeryUIMechanismRequest(IPlayerSession session, Dictionary<string, int> options)
        {
            UserInterface?.SendMessage(new RequestMechanismSurgeryUIMessage(options), session);
        }

        private void CloseSurgeryUI(IPlayerSession session)
        {
            UserInterface?.Close(session);
        }

        private void CloseAllSurgeryUIs()
        {
            UserInterface?.CloseAll();
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
        ///     <see cref="IBodyPart"/> that can be operated on.
        /// </summary>
        private void HandleReceiveBodyPart(int key)
        {
            if (_performerCache == null ||
                !_performerCache.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            CloseSurgeryUI(actor.playerSession);
            // TODO: sanity checks to see whether user is in range, user is still able-bodied, target is still the same, etc etc
            if (!_optionsCache.TryGetValue(key, out var targetObject) ||
                _bodyCache == null)
            {
                NotUsefulAnymorePopup();
                return;
            }

            var target = (IBodyPart) targetObject!;

            // TODO BODY Reconsider
            if (!target.AttemptSurgery(_surgeryType, _bodyCache, this, _performerCache))
            {
                NotUsefulAnymorePopup();
            }
        }

        /// <summary>
        ///     Called after the client chooses from a list of possible
        ///     <see cref="IMechanism"/> to choose from.
        /// </summary>
        private void HandleReceiveMechanism(int key)
        {
            // TODO: sanity checks to see whether user is in range, user is still able-bodied, target is still the same, etc etc
            if (!_optionsCache.TryGetValue(key, out var targetObject) ||
                _performerCache == null ||
                !_performerCache.TryGetComponent(out IActorComponent? actor))
            {
                NotUsefulAnymorePopup();
                return;
            }

            var target = targetObject as MechanismComponent;

            CloseSurgeryUI(actor.playerSession);
            _callbackCache?.Invoke(target, _bodyCache, this, _performerCache);
        }

        private void NotUsefulPopup()
        {
            _bodyCache?.Owner.PopupMessage(_performerCache,
                Loc.GetString("You see no useful way to use {0:theName}.", Owner));
        }

        private void NotUsefulAnymorePopup()
        {
            _bodyCache?.Owner.PopupMessage(_performerCache,
                Loc.GetString("You see no useful way to use {0:theName} anymore.", Owner));
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _surgeryType, "surgeryType", SurgeryType.Incision);
            serializer.DataField(ref _baseOperateTime, "baseOperateTime", 5);
        }
    }
}
