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
using Robust.Shared.Log;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.GameObjects;
using System.Diagnostics;

namespace Content.Server.BodySystem {

    /// <summary>
    ///    Component containing the data for a dropped Mechanism entity.
    /// </summary>
    [RegisterComponent]
    public class DroppedMechanismComponent : Component, IAfterInteract
    {

#pragma warning disable 649
        [Dependency] private readonly ISharedNotifyManager _sharedNotifyManager;
        [Dependency] private readonly IRobustRandom _random;
        [Dependency] private IPrototypeManager _prototypeManager;
#pragma warning restore 649

        public sealed override string Name => "DroppedMechanism";

        [ViewVariables]
        public Mechanism ContainedMechanism { get; private set; }

        private BoundUserInterface _userInterface;
        private Dictionary<object, object> _optionsCache = new Dictionary<object, object>();
        private IEntity _performerCache;
        private BodyManagerComponent _bodyManagerComponentCache;

        public override void Initialize()
        {
            base.Initialize();
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>().GetBoundUserInterface(GenericSurgeryUiKey.Key);
            _userInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
        }

        public void InitializeDroppedMechanism(Mechanism data)
        {
            ContainedMechanism = data;
            Owner.Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(ContainedMechanism.Name);
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
                SendBodyPartListToUser(eventArgs, bodyManager);
            }
            else if (eventArgs.Target.TryGetComponent<DroppedBodyPartComponent>(out DroppedBodyPartComponent droppedBodyPart))
            {
                if (droppedBodyPart.ContainedBodyPart == null)
                {
                    Logger.Debug("Installing a mechanism was attempted on an IEntity with a DroppedBodyPartComponent that doesn't have a BodyPart in it!");
                    throw new InvalidOperationException("A DroppedBodyPartComponent exists without a BodyPart in it!");
                }
                if (!droppedBodyPart.ContainedBodyPart.TryInstallDroppedMechanism(this))
                {
                    _sharedNotifyManager.PopupMessage(eventArgs.Target, eventArgs.User, "You can't fit it in!");
                }
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            //This is a temporary way to have spawnable hard-coded DroppedMechanismComponent prototypes
            //In the future (when it becomes possible) DroppedMechanismComponent should be auto-generated from the Mechanism prototypes
            string debugLoadMechanismData = "";
            base.ExposeData(serializer);
            serializer.DataField(ref debugLoadMechanismData, "debugLoadMechanismData", "");
            if (serializer.Reading && debugLoadMechanismData != "")
            {
                _prototypeManager.TryIndex(debugLoadMechanismData, out MechanismPrototype data);
                InitializeDroppedMechanism(new Mechanism(data));
            }
        }



        private void SendBodyPartListToUser(AfterInteractEventArgs eventArgs, BodyManagerComponent bodyManager)
        {
            var toSend = new Dictionary<string, object>(); //Create dictionary to send to client (text to be shown : data sent back if selected)
            foreach (var (key, value) in bodyManager.PartDictionary)
            { //For each limb in the target, add it to our cache if it is a valid option.
                if (value.CanInstallMechanism(ContainedMechanism))
                {
                    int randomKey = _random.Next(Int32.MinValue, Int32.MaxValue);
                    _optionsCache.Add(randomKey, value);
                    toSend.Add(key + ": " + value.Name, randomKey);
                }
            }
            if (_optionsCache.Count > 0)
            {
                OpenSurgeryUI(eventArgs.User.GetComponent<BasicActorComponent>().playerSession);
                UpdateSurgeryUI(eventArgs.User.GetComponent<BasicActorComponent>().playerSession, SurgeryUIMessageType.SelectBodyPart, toSend);
                _performerCache = eventArgs.User;
                _bodyManagerComponentCache = bodyManager;
            }
            else //If surgery cannot be performed, show message saying so.
            {
                _sharedNotifyManager.PopupMessage(eventArgs.Target, eventArgs.User, "You see no way to install the " + Owner.Name + ".");
            }
        }

        /// <summary>
        ///     Called after the client chooses from a list of possible BodyParts that can be operated on. 
        /// </summary>
        private void HandleReceiveBodyPart(int key)
        {
            CloseSurgeryUI(_performerCache.GetComponent<BasicActorComponent>().playerSession);
            //TODO: sanity checks to see whether user is in range, user is still able-bodied, target is still the same, etc etc
            if (!_optionsCache.TryGetValue(key, out object targetObject))
            {
                _sharedNotifyManager.PopupMessage(_bodyManagerComponentCache.Owner, _performerCache, "You see no useful way to use the " + Owner.Name + " anymore.");
            }
            BodyPart target = targetObject as BodyPart;
            if (!target.TryInstallDroppedMechanism(this))
            {
                _sharedNotifyManager.PopupMessage(_bodyManagerComponentCache.Owner, _performerCache, "You can't fit it in!");
            }
            else
            {
                _sharedNotifyManager.PopupMessage(_bodyManagerComponentCache.Owner, _performerCache, "You jam the " + ContainedMechanism.Name + " inside him.");
            }
        }




        public void OpenSurgeryUI(IPlayerSession session)
        {
            _userInterface.Open(session);
        }
        public void UpdateSurgeryUI(IPlayerSession session, SurgeryUIMessageType messageType, Dictionary<string, object> options)
        {
            _userInterface.SendMessage(new UpdateSurgeryUIMessage(messageType, options), session);
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
                case ReceiveSurgeryUIMessage msg:
                    HandleReceiveSurgeryUIMessage(msg);
                    break;
            }
        }
        private void HandleReceiveSurgeryUIMessage(ReceiveSurgeryUIMessage msg)
        {
            if (msg.MessageType == SurgeryUIMessageType.SelectBodyPart)
                HandleReceiveBodyPart((int) msg.SelectedOptionData);
        }
    }
}

