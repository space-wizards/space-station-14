using System;
using System.Collections.Generic;
using Content.Server.BodySystem;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.BodySystem;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.Interfaces;
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
    public class ServerSurgeryToolComponent : SharedSurgeryToolComponent, IAfterAttack
    {
#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly IPhysicsManager _physicsManager;
        [Dependency] private readonly ISharedNotifyManager _sharedNotifyManager;
#pragma warning restore 649

        public HashSet<IPlayerSession> SubscribedSessions = new HashSet<IPlayerSession>();
        private Dictionary<string, BodyPart> _surgeryOptionsCache = new Dictionary<string, BodyPart>();
        private IEntity _performerCache;
        private BodyManagerComponent _bodyManagerComponentCache;

        void IAfterAttack.AfterAttack(AfterAttackEventArgs eventArgs)
        {
            if (eventArgs.Attacked == null)
                return;
            if (eventArgs.Attacked.TryGetComponent<BodyManagerComponent>(out BodyManagerComponent bodyManager))
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
                    _bodyManagerComponentCache = bodyManager;
                }
                else
                {
                    _sharedNotifyManager.PopupMessage(eventArgs.Attacked, eventArgs.User, "You see no useful way to use the " + Owner.Name + ".");
                }
            }
            if (eventArgs.Attacked.TryGetComponent<DroppedBodyPartComponent>(out DroppedBodyPartComponent droppedBodyPart))
            {
                if (droppedBodyPart.ContainedBodyPart == null)
                {
                    Logger.Debug("Surgery was attempted on an IEntity with a DroppedBodyPartComponent that doesn't have a BodyPart in it!");
                    throw new InvalidOperationException("A DroppedBodyPartComponent exists without a BodyPart in it!");
                }
                if (droppedBodyPart.ContainedBodyPart.SurgeryCheck(_surgeryToolClass)) //If surgery can be performed...
                {
                    if (!droppedBodyPart.ContainedBodyPart.AttemptSurgery(_surgeryToolClass, droppedBodyPart, eventArgs.User)) //...do the surgery.
                    {
                        Logger.Debug("Error when trying to perform surgery on bodypart " + eventArgs.User.Name + "!");
                        throw new InvalidOperationException();
                    }
                }
                else
                {
                    _sharedNotifyManager.PopupMessage(eventArgs.Attacked, eventArgs.User,"You see no useful way to use the " + Owner.Name + ".");
                }
            }
        }

        /// <summary>
        /// Called after the user selects a surgery target. 
        /// </summary>
        void PerformSurgeryOnBodyManagerSlot(string targetSlot)
        {
            //TODO: sanity checks to see whether user is in range, body is still same, etc etc
            if (!_surgeryOptionsCache.TryGetValue(targetSlot, out BodyPart target))
            {
                Logger.Debug("Error when trying to perform surgery on bodypart in slot " + targetSlot + ": it was not found!");
                throw new InvalidOperationException();
            }
            if (!target.AttemptSurgery(_surgeryToolClass, _bodyManagerComponentCache, _performerCache))
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
                    PerformSurgeryOnBodyManagerSlot(msg.TargetSlot);
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
