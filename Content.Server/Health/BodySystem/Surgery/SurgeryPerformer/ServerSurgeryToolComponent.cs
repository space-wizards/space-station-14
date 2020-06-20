using System;
using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.BodySystem;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Players;
using Robust.Shared.Serialization;

namespace Content.Server.BodySystem
{
    //TODO: add checks to close UI if user walks too far away from tool or target.

    /// <summary>
    ///     Server-side component representing a generic tool capable of performing surgery. For instance, the scalpel.
    /// </summary>
    [RegisterComponent]
    public class ServerSurgeryToolComponent : SharedSurgeryToolComponent, ISurgeon, IAfterInteract
    {
        
#pragma warning disable 649
        [Dependency] private readonly ISharedNotifyManager _sharedNotifyManager;
#pragma warning restore 649

        public float BaseOperationTime { get => _baseOperateTime; set => _baseOperateTime = value; }
        private float _baseOperateTime;
        private SurgeryType _surgeryType;
        private HashSet<IPlayerSession> _subscribedSessions = new HashSet<IPlayerSession>();
        private Random _random = new Random();

        private Dictionary<object, object> _optionsCache = new Dictionary<object, object>();
        private IEntity _performerCache;
        private BodyManagerComponent _bodyManagerComponentCache;
        private ISurgeon.MechanismRequestCallback _callbackCache;

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null)
                return;

            CloseAllSurgeryUIs();
            _optionsCache.Clear();
            _performerCache = null;
            _bodyManagerComponentCache = null;
            _callbackCache = null;

            if (eventArgs.Target.TryGetComponent<BodyManagerComponent>(out BodyManagerComponent bodyManager)) //Attempt surgery on a BodyManagerComponent by sending a list of operatable BodyParts to the client to choose from
            {
                var toSend = new Dictionary<string, object>(); //Create dictionary to send to client (text to be shown : data sent back if selected)
                foreach (var(key, value) in bodyManager.PartDictionary) { //For each limb in the target, add it to our cache if it is a valid option.
                    if (value.SurgeryCheck(_surgeryType)) 
                    {
                        int randomKey = _random.Next(Int32.MinValue, Int32.MaxValue);
                        _optionsCache.Add(randomKey, value);
                        toSend.Add(key + ": " + value.Name, randomKey);
                    }
                }
                if (_optionsCache.Count > 0)
                {
                    OpenSurgeryUI(eventArgs.User, SurgeryUIMessageType.SelectBodyPart); 
                    UpdateSurgeryUI(eventArgs.User, toSend);
                    _performerCache = eventArgs.User; //Also, cache the data.
                    _bodyManagerComponentCache = bodyManager;
                }
                else //If surgery cannot be performed, show message saying so.
                {
                    _sharedNotifyManager.PopupMessage(eventArgs.Target, eventArgs.User, "You see no useful way to use the " + Owner.Name + ".");
                }
            }
            else if (eventArgs.Target.TryGetComponent<DroppedBodyPartComponent>(out DroppedBodyPartComponent droppedBodyPart)) //Attempt surgery on a DroppedBodyPart - there's only one possible target so no need for selection UI
            {
                if (droppedBodyPart.ContainedBodyPart == null) //Throw error if the DroppedBodyPart has no data in it.
                {
                    Logger.Debug("Surgery was attempted on an IEntity with a DroppedBodyPartComponent that doesn't have a BodyPart in it!");
                    throw new InvalidOperationException("A DroppedBodyPartComponent exists without a BodyPart in it!");
                }
                if (droppedBodyPart.ContainedBodyPart.SurgeryCheck(_surgeryType)) //If surgery can be performed...
                {
                    if (!droppedBodyPart.ContainedBodyPart.AttemptSurgery(_surgeryType, droppedBodyPart, this, eventArgs.User)) //...do the surgery.
                    {
                        Logger.Debug("Error when trying to perform surgery on bodypart " + eventArgs.User.Name + "!"); //Log error if the surgery fails somehow.
                        throw new InvalidOperationException();
                    }
                }
                else //If surgery cannot be performed, show message saying so.
                {
                    _sharedNotifyManager.PopupMessage(eventArgs.Target, eventArgs.User,"You see no useful way to use the " + Owner.Name + ".");
                }
            }
        }



        /// <summary>
        ///     Called after the client chooses from a list of possible BodyParts that can be operated on. 
        /// </summary>
        private void HandleReceiveBodyPart(int key)
        {
            CloseSurgeryUI(_performerCache);
            //TODO: sanity checks to see whether user is in range, user is still able-bodied, target is still the same, etc etc
            if (!_optionsCache.TryGetValue(key, out object targetObject))
            {
                _sharedNotifyManager.PopupMessage(_bodyManagerComponentCache.Owner, _performerCache, "You see no useful way to use the " + Owner.Name + " anymore.");
            }
            BodyPart target = targetObject as BodyPart;
            if (!target.AttemptSurgery(_surgeryType, _bodyManagerComponentCache, this, _performerCache))
            {
                _sharedNotifyManager.PopupMessage(_bodyManagerComponentCache.Owner, _performerCache, "You see no useful way to use the " + Owner.Name + " anymore.");
            }
            
        }

        /// <summary>
        ///     Called after the client chooses from a list of possible Mechanisms to choose from.
        /// </summary>
        private void HandleReceiveMechanism(int key)
        {
            //TODO: sanity checks to see whether user is in range, user is still able-bodied, target is still the same, etc etc
            if (!_optionsCache.TryGetValue(key, out object targetObject))
            {
                _sharedNotifyManager.PopupMessage(_bodyManagerComponentCache.Owner, _performerCache, "You see no useful way to use the " + Owner.Name + " anymore.");
            }
            Mechanism target = targetObject as Mechanism;
            CloseSurgeryUI(_performerCache);
            _callbackCache(target, _bodyManagerComponentCache, this, _performerCache);
        }



        public void RequestMechanism(List<Mechanism> options, ISurgeon.MechanismRequestCallback callback)
        {
            Dictionary<string, object> toSend = new Dictionary<string, object>();
            foreach (Mechanism mechanism in options)
            {
                int randomKey = _random.Next(Int32.MinValue, Int32.MaxValue);
                _optionsCache.Add(randomKey, mechanism);
                toSend.Add(mechanism.Name, randomKey);
            }
            if (_optionsCache.Count > 0)
            {
                OpenSurgeryUI(_performerCache, SurgeryUIMessageType.SelectMechanism);
                UpdateSurgeryUI(_performerCache, toSend);
                _callbackCache = callback;
            }
            else
            {
                Logger.Debug("Error on callback from mechanisms: there were no viable options to choose from!");
                throw new InvalidOperationException();
            }
        }



        public void OpenSurgeryUI(IEntity character, SurgeryUIMessageType messageType)
        {
            var user_session = character.GetComponent<BasicActorComponent>().playerSession;
            SubscribeSession(user_session);
            SendNetworkMessage(new OpenSurgeryUIMessage(messageType), user_session.ConnectedClient);
        }
        public void UpdateSurgeryUI(IEntity character, Dictionary<string, object> options)
        {
            var user_session = character.GetComponent<BasicActorComponent>().playerSession;
            if (user_session.AttachedEntity == null)
            {
                UnsubscribeSession(user_session);
                return;
            }
            SendNetworkMessage(new UpdateSurgeryUIMessage(options), user_session.ConnectedClient);
        }
        public void CloseSurgeryUI(IEntity character)
        {
            var user_session = character.GetComponent<BasicActorComponent>().playerSession;
            UnsubscribeSession(user_session);
        }
        public void CloseAllSurgeryUIs()
        {
            foreach (var session in _subscribedSessions)
            {
                UnsubscribeSession(session);
            }
        }
        private void HandleReceiveSurgeryUIMessage(ReceiveSurgeryUIMessage msg)
        {
            if(msg.MessageType == SurgeryUIMessageType.SelectBodyPart)
                HandleReceiveBodyPart((int)msg.SelectedOptionData);
            else if(msg.MessageType == SurgeryUIMessageType.SelectMechanism)
                HandleReceiveMechanism((int)msg.SelectedOptionData);
        }




        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, channel, session);

            if (session == null)
            {
                throw new ArgumentException(nameof(session));
            }

            switch (message)
            {
                case CloseSurgeryUIMessage msg:
                    UnsubscribeSession(session as IPlayerSession);
                    break;
                case ReceiveSurgeryUIMessage msg:
                    HandleReceiveSurgeryUIMessage(msg);
                    break;
            }
        }
        private void SubscribeSession(IPlayerSession session)
        {
            if (!_subscribedSessions.Contains(session))
            {
                session.PlayerStatusChanged += HandlePlayerSessionChangeEvent;
                _subscribedSessions.Add(session);
            }
        }
        private void UnsubscribeSession(IPlayerSession session)
        {
            if (_subscribedSessions.Contains(session))
            {
                _subscribedSessions.Remove(session);
                SendNetworkMessage(new CloseSurgeryUIMessage(), session.ConnectedClient);
            }
        }
        private void HandlePlayerSessionChangeEvent(object obj, SessionStatusEventArgs SSEA)
        {
            if (SSEA.NewStatus != SessionStatus.InGame)
            {
                UnsubscribeSession(SSEA.Session);
            }
        }


        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _surgeryType, "surgeryType", SurgeryType.Incision);
            serializer.DataField(ref _baseOperateTime, "baseOperateTime", 5);
        }
    }
}
