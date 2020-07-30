using System;
using System.Collections.Generic;
using Content.Server.Body;
using Content.Server.Body.Mechanisms;
using Content.Server.Body.Surgery;
using Content.Shared.Body;
using Content.Shared.Body.Surgery;
using Content.Shared.GameObjects;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Body
{
    // TODO: add checks to close UI if user walks too far away from tool or target.

    /// <summary>
    ///     Server-side component representing a generic tool capable of performing surgery. For instance, the scalpel.
    /// </summary>
    [RegisterComponent]
    public class SurgeryToolComponent : Component, ISurgeon, IAfterInteract
    {
#pragma warning disable 649
        [Dependency] private readonly ISharedNotifyManager _sharedNotifyManager;
#pragma warning restore 649

        public override string Name => "SurgeryTool";
        public override uint? NetID => ContentNetIDs.SURGERY;

        private readonly Dictionary<int, object> _optionsCache = new Dictionary<int, object>();

        private float _baseOperateTime;

        private BodyManagerComponent _bodyManagerComponentCache;

        private ISurgeon.MechanismRequestCallback _callbackCache;

        private int _idHash;

        private IEntity _performerCache;

        private HashSet<IPlayerSession> _subscribedSessions = new HashSet<IPlayerSession>(); // TODO

        private SurgeryType _surgeryType;

        private BoundUserInterface _userInterface;

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null)
            {
                return;
            }

            CloseAllSurgeryUIs();
            _optionsCache.Clear();
            _performerCache = null;
            _bodyManagerComponentCache = null;
            _callbackCache = null;

            // Attempt surgery on a BodyManagerComponent by sending a list of operable BodyParts to the client to choose from
            if (eventArgs.Target.TryGetComponent(out BodyManagerComponent body))
            {
                // Create dictionary to send to client (text to be shown : data sent back if selected)
                var toSend = new Dictionary<string, int>();

                foreach (var (key, value) in body.PartDictionary)
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
                    OpenSurgeryUI(eventArgs.User.GetComponent<BasicActorComponent>().playerSession);
                    UpdateSurgeryUIBodyPartRequest(eventArgs.User.GetComponent<BasicActorComponent>().playerSession,
                        toSend);
                    _performerCache = eventArgs.User; // Also, cache the data.
                    _bodyManagerComponentCache = body;
                }
                else // If surgery cannot be performed, show message saying so.
                {
                    SendNoUsefulWayToUsePopup();
                }
            }
            else if (eventArgs.Target.TryGetComponent<DroppedBodyPartComponent>(out var droppedBodyPart))
            {
                // Attempt surgery on a DroppedBodyPart - there's only one possible target so no need for selection UI
                _performerCache = eventArgs.User;

                if (droppedBodyPart.ContainedBodyPart == null)
                {
                    // Throw error if the DroppedBodyPart has no data in it.
                    Logger.Debug(
                        "Surgery was attempted on an IEntity with a DroppedBodyPartComponent that doesn't have a BodyPart in it!");
                    throw new InvalidOperationException("A DroppedBodyPartComponent exists without a BodyPart in it!");
                }

                // If surgery can be performed...
                if (droppedBodyPart.ContainedBodyPart.SurgeryCheck(_surgeryType))
                {
                    //...do the surgery.
                    if (!droppedBodyPart.ContainedBodyPart.AttemptSurgery(_surgeryType, droppedBodyPart, this,
                        eventArgs.User))
                    {
                        // Log error if the surgery fails somehow.
                        Logger.Debug($"Error when trying to perform surgery on bodypart {eventArgs.User.Name}!");
                        throw new InvalidOperationException();
                    }
                }
                else // If surgery cannot be performed, show message saying so.
                {
                    SendNoUsefulWayToUsePopup();
                }
            }
        }

        public float BaseOperationTime { get => _baseOperateTime; set => _baseOperateTime = value; }

        public void RequestMechanism(List<Mechanism> options, ISurgeon.MechanismRequestCallback callback)
        {
            var toSend = new Dictionary<string, int>();
            foreach (var mechanism in options)
            {
                _optionsCache.Add(_idHash, mechanism);
                toSend.Add(mechanism.Name, _idHash++);
            }

            if (_optionsCache.Count > 0)
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
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>()
                .GetBoundUserInterface(GenericSurgeryUiKey.Key);
            _userInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
        }

        private void OpenSurgeryUI(IPlayerSession session)
        {
            _userInterface.Open(session);
        }

        private void UpdateSurgeryUIBodyPartRequest(IPlayerSession session, Dictionary<string, int> options)
        {
            _userInterface.SendMessage(new RequestBodyPartSurgeryUIMessage(options), session);
        }

        private void UpdateSurgeryUIMechanismRequest(IPlayerSession session, Dictionary<string, int> options)
        {
            _userInterface.SendMessage(new RequestMechanismSurgeryUIMessage(options), session);
        }

        private void CloseSurgeryUI(IPlayerSession session)
        {
            _userInterface.Close(session);
        }

        private void CloseAllSurgeryUIs()
        {
            _userInterface.CloseAll();
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage message)
        {
            switch (message.Message)
            {
                case ReceiveBodyPartSurgeryUIMessage msg:
                    HandleReceiveBodyPart(msg.SelectedOptionID);
                    break;
                case ReceiveMechanismSurgeryUIMessage msg:
                    HandleReceiveMechanism(msg.SelectedOptionID);
                    break;
            }
        }

        /// <summary>
        ///     Called after the client chooses from a list of possible <see cref="BodyPart"/>
        ///     that can be operated on.
        /// </summary>
        private void HandleReceiveBodyPart(int key)
        {
            CloseSurgeryUI(_performerCache.GetComponent<BasicActorComponent>().playerSession);
            // TODO: sanity checks to see whether user is in range, user is still able-bodied, target is still the same, etc etc
            if (!_optionsCache.TryGetValue(key, out var targetObject))
            {
                SendNoUsefulWayToUseAnymorePopup();
            }

            var target = targetObject as BodyPart;

            if (!target.AttemptSurgery(_surgeryType, _bodyManagerComponentCache, this, _performerCache))
            {
                SendNoUsefulWayToUseAnymorePopup();
            }
        }

        /// <summary>
        ///     Called after the client chooses from a list of possible <see cref="Mechanism">Mechanisms</see> to choose from.
        /// </summary>
        private void HandleReceiveMechanism(int key)
        {
            // TODO: sanity checks to see whether user is in range, user is still able-bodied, target is still the same, etc etc
            if (!_optionsCache.TryGetValue(key, out var targetObject))
            {
                SendNoUsefulWayToUseAnymorePopup();
            }

            var target = targetObject as Mechanism;
            CloseSurgeryUI(_performerCache.GetComponent<BasicActorComponent>().playerSession);
            _callbackCache(target, _bodyManagerComponentCache, this, _performerCache);
        }

        private void SendNoUsefulWayToUsePopup()
        {
            _sharedNotifyManager.PopupMessage(_bodyManagerComponentCache.Owner, _performerCache,
                Loc.GetString("You see no useful way to use {0:theName}.", Owner));
        }

        private void SendNoUsefulWayToUseAnymorePopup()
        {
            _sharedNotifyManager.PopupMessage(_bodyManagerComponentCache.Owner, _performerCache,
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
