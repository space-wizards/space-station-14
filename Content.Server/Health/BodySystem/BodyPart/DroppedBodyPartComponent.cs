using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using Content.Shared.BodySystem;
using Robust.Shared.ViewVariables;
using System.Globalization;
using Robust.Server.GameObjects;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.Player;
using Content.Shared.Interfaces;
using Robust.Shared.Interfaces.Random;
using System.Linq;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Robust.Shared.Localization;

namespace Content.Server.BodySystem
{

    /// <summary>
    ///    Component representing a dropped, tangible <see cref="BodyPart"/> entity.
    /// </summary>
    [RegisterComponent]
    public class DroppedBodyPartComponent : Component, IAfterInteract, IBodyPartContainer
    {

#pragma warning disable 649
        [Dependency] private readonly ISharedNotifyManager _sharedNotifyManager;
#pragma warning restore 649

        public sealed override string Name => "DroppedBodyPart";

        [ViewVariables]
        public BodyPart ContainedBodyPart { get; set; }

        private BoundUserInterface _userInterface;
        private Dictionary<int, object> _optionsCache = new Dictionary<int, object>();
        private IEntity _performerCache;
        private BodyManagerComponent _bodyManagerComponentCache;
        private int _idHash = 0;

        public override void Initialize()
        {
            base.Initialize();
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>().GetBoundUserInterface(GenericSurgeryUiKey.Key);
            _userInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
        }

        public void TransferBodyPartData(BodyPart data)
        {
            ContainedBodyPart = data;
            Owner.Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(ContainedBodyPart.Name);
            if (Owner.TryGetComponent<SpriteComponent>(out SpriteComponent component))
            {
                component.LayerSetRSI(0, data.RSIPath);
                component.LayerSetState(0, data.RSIState);
            }
        }

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null)
                return;

            CloseAllSurgeryUIs();
            _optionsCache.Clear();
            _performerCache = null;
            _bodyManagerComponentCache = null;

            if (eventArgs.Target.TryGetComponent<BodyManagerComponent>(out BodyManagerComponent bodyManager))
            {
                SendBodySlotListToUser(eventArgs, bodyManager);
            }
        }

        private void SendBodySlotListToUser(AfterInteractEventArgs eventArgs, BodyManagerComponent bodyManager)
        {
            var toSend = new Dictionary<string, int>(); //Create dictionary to send to client (text to be shown : data sent back if selected)

            //Here we are trying to grab a list of all empty BodySlots adjancent to an existing BodyPart that can be attached to. i.e. an empty left hand slot, connected to an occupied left arm slot would be valid.
            List<string> unoccupiedSlots = bodyManager.AllSlots.ToList().Except(bodyManager.OccupiedSlots.ToList()).ToList();
            foreach (string slot in unoccupiedSlots)
            {
                if (bodyManager.TryGetSlotType(slot, out BodyPartType typeResult) && typeResult == ContainedBodyPart.PartType)
                {
                    if (bodyManager.TryGetBodyPartConnections(slot, out List<BodyPart> bodypartResult))
                    {
                        foreach (BodyPart connectedPart in bodypartResult)
                        {
                            if (connectedPart.CanAttachBodyPart(ContainedBodyPart))
                            {
                                _optionsCache.Add(_idHash, slot);
                                toSend.Add(slot, _idHash++);
                            }
                        }
                    }
                }
            }
            if (_optionsCache.Count > 0)
            {
                OpenSurgeryUI(eventArgs.User.GetComponent<BasicActorComponent>().playerSession);
                UpdateSurgeryUIBodyPartSlotRequest(eventArgs.User.GetComponent<BasicActorComponent>().playerSession, toSend);
                _performerCache = eventArgs.User;
                _bodyManagerComponentCache = bodyManager;
            }
            else //If surgery cannot be performed, show message saying so.
            {
                _sharedNotifyManager.PopupMessage(eventArgs.Target, eventArgs.User, Loc.GetString("You see no way to install the {0}.", Owner.Name));
            }
        }

        /// <summary>
        ///     Called after the client chooses from a list of possible BodyPartSlots to install the limb on.
        /// </summary>
        private void HandleReceiveBodyPartSlot(int key)
        {
            CloseSurgeryUI(_performerCache.GetComponent<BasicActorComponent>().playerSession);
            //TODO: sanity checks to see whether user is in range, user is still able-bodied, target is still the same, etc etc
            if (!_optionsCache.TryGetValue(key, out object targetObject))
            {
                _sharedNotifyManager.PopupMessage(_bodyManagerComponentCache.Owner, _performerCache, Loc.GetString("You see no useful way to attach the {0} anymore.", Owner.Name));
            }
            string target = targetObject as string;
            if (!_bodyManagerComponentCache.InstallDroppedBodyPart(this, target))
            {
                _sharedNotifyManager.PopupMessage(_bodyManagerComponentCache.Owner, _performerCache, Loc.GetString("You can't attach it!"));
            }
            else
            {
                _sharedNotifyManager.PopupMessage(_bodyManagerComponentCache.Owner, _performerCache, Loc.GetString("You attach the {0}.", ContainedBodyPart.Name));
            }
        }


        public void OpenSurgeryUI(IPlayerSession session)
        {
            _userInterface.Open(session);
        }
        public void UpdateSurgeryUIBodyPartSlotRequest(IPlayerSession session, Dictionary<string, int> options)
        {
            _userInterface.SendMessage(new RequestBodyPartSlotSurgeryUIMessage(options), session);
        }
        public void CloseSurgeryUI(IPlayerSession session)
        {
            _userInterface.Close(session);
        }
        public void CloseAllSurgeryUIs()
        {
            _userInterface.CloseAll();
        }


        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage message)
        {
            switch (message.Message)
            {
                case ReceiveBodyPartSlotSurgeryUIMessage msg:
                    HandleReceiveBodyPartSlot(msg.SelectedOptionID);
                    break;
            }
        }
    }
}
