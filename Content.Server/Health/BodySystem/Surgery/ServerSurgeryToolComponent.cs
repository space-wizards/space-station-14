using System;
using System.Collections.Generic;
using Content.Server.BodySystem;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Utility;
using Content.Shared.BodySystem;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Items;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Weapon.Melee
{

    [RegisterComponent]
    public class ServerSurgeryToolComponent : SharedSurgeryToolComponent, IAfterInteract
    {
        public HashSet<IPlayerSession> SubscribedSessions = new HashSet<IPlayerSession>();
        private Dictionary<string, BodyPart> _surgeryOptionsCache = new Dictionary<string, BodyPart>();
        private BodyManagerComponent _targetCache;
        private IEntity _performerCache;

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!InteractionChecks.InRangeUnobstructed(eventArgs)) return;

            if (eventArgs.Target == null)
                return;
            if (eventArgs.Target.TryGetComponent<BodySystem.BodyManagerComponent>(out BodySystem.BodyManagerComponent bodyManager))
            {
                _surgeryOptionsCache.Clear();
                var toSend = new Dictionary<string, string>();
                foreach (var(key, value) in bodyManager.PartDictionary) {
                    if (value.SurgeryCheck(_surgeryToolClass))
                    {
                        _surgeryOptionsCache.Add(key, value);
                        toSend.Add(key, value.Name);
                    }
                }
                if (_surgeryOptionsCache.Count > 0)
                {
                    OpenSurgeryUI(eventArgs.User);
                    UpdateSurgeryUI(eventArgs.User, toSend);
                    _performerCache = eventArgs.User;
                    _targetCache = bodyManager;
                }
            }
        }

        /// <summary>
        /// Called after the user selects a surgery target.
        /// </summary>
        void PerformSurgery(SelectSurgeryUIMessage msg)
        {
            //TODO: sanity checks to see whether user is in range, body is still same, etc etc
            if (!_surgeryOptionsCache.TryGetValue(msg.TargetSlot, out BodyPart target))
            {
                Logger.Debug("Error when trying to perform surgery on bodypart in slot " + msg.TargetSlot + ": it was not found!");
                throw new InvalidOperationException();
            }
            if (!target.AttemptSurgery(_surgeryToolClass, _targetCache, _performerCache))
            {
                Logger.Debug("Error when trying to perform surgery on bodypart " + target.Name + "!");
                throw new InvalidOperationException();
            }
            CloseSurgeryUI(_performerCache);
        }


        public void OpenSurgeryUI(IEntity character)
        {
            var user_session = character.GetComponent<BasicActorComponent>().playerSession;
            SubscribeSession(user_session);
            SendNetworkMessage(new OpenSurgeryUIMessage(), user_session.ConnectedClient);
        }
        public void UpdateSurgeryUI(IEntity character, Dictionary<string, string> options)
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
            SubscribeSession(user_session);
            SendNetworkMessage(new CloseSurgeryUIMessage(), user_session.ConnectedClient);
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
                case SelectSurgeryUIMessage msg:
                    PerformSurgery(msg);
                    break;
            }
        }


        public void SubscribeSession(IPlayerSession session)
        {
            if (!SubscribedSessions.Contains(session))
            {
                session.PlayerStatusChanged += HandlePlayerSessionChangeEvent;
                SubscribedSessions.Add(session);
            }
        }
        public void UnsubscribeSession(IPlayerSession session)
        {
            if (SubscribedSessions.Contains(session))
            {
                SubscribedSessions.Remove(session);
                SendNetworkMessage(new CloseSurgeryUIMessage(), session.ConnectedClient);
            }
        }
        public void HandlePlayerSessionChangeEvent(object obj, SessionStatusEventArgs SSEA)
        {
            if (SSEA.NewStatus != SessionStatus.InGame)
            {
                UnsubscribeSession(SSEA.Session);
            }
        }
    }
}
