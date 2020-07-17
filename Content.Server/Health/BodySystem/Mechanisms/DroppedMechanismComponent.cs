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
using Content.Server.Health.BodySystem;
using Content.Server.Health.BodySystem.BodyParts;
using Content.Server.Health.BodySystem.Mechanisms;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Robust.Shared.Localization;

namespace Content.Server.BodySystem {

    /// <summary>
    ///    Component representing a dropped, tangible <see cref="Mechanism"/> entity.
    /// </summary>
    [RegisterComponent]
    public class DroppedMechanismComponent : Component, IAfterInteract
    {

#pragma warning disable 649
        [Dependency] private readonly ISharedNotifyManager _sharedNotifyManager;
        [Dependency] private IPrototypeManager _prototypeManager;
#pragma warning restore 649

        public sealed override string Name => "DroppedMechanism";

        [ViewVariables]
        public Mechanism ContainedMechanism { get; private set; }

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
                    _sharedNotifyManager.PopupMessage(eventArgs.Target, eventArgs.User, Loc.GetString("You can't fit it in!"));
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
            var toSend = new Dictionary<string, int>(); //Create dictionary to send to client (text to be shown : data sent back if selected)
            foreach (var (key, value) in bodyManager.PartDictionary)
            { //For each limb in the target, add it to our cache if it is a valid option.
                if (value.CanInstallMechanism(ContainedMechanism))
                {
                    _optionsCache.Add(_idHash, value);
                    toSend.Add(key + ": " + value.Name, _idHash++);
                }
            }
            if (_optionsCache.Count > 0)
            {
                OpenSurgeryUI(eventArgs.User.GetComponent<BasicActorComponent>().playerSession);
                UpdateSurgeryUIBodyPartRequest(eventArgs.User.GetComponent<BasicActorComponent>().playerSession, toSend);
                _performerCache = eventArgs.User;
                _bodyManagerComponentCache = bodyManager;
            }
            else //If surgery cannot be performed, show message saying so.
            {
                _sharedNotifyManager.PopupMessage(eventArgs.Target, eventArgs.User, Loc.GetString("You see no way to install the {0}.", Owner.Name));
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
                _sharedNotifyManager.PopupMessage(_bodyManagerComponentCache.Owner, _performerCache, Loc.GetString("You see no useful way to use the {0} anymore.", Owner.Name));
            }
            BodyPart target = targetObject as BodyPart;
            if (!target.TryInstallDroppedMechanism(this))
            {
                _sharedNotifyManager.PopupMessage(_bodyManagerComponentCache.Owner, _performerCache, Loc.GetString("You can't fit it in!"));
            }
            else
            {
                _sharedNotifyManager.PopupMessage(_bodyManagerComponentCache.Owner, _performerCache, Loc.GetString("You jam the {1} inside {0:them}.", _performerCache, ContainedMechanism.Name));
            }
        }




        public void OpenSurgeryUI(IPlayerSession session)
        {
            _userInterface.Open(session);
        }
        public void UpdateSurgeryUIBodyPartRequest(IPlayerSession session, Dictionary<string, int> options)
        {
            _userInterface.SendMessage(new RequestBodyPartSurgeryUIMessage(options), session);
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
                case ReceiveBodyPartSurgeryUIMessage msg:
                    HandleReceiveBodyPart(msg.SelectedOptionID);
                    break;
            }
        }
    }
}

