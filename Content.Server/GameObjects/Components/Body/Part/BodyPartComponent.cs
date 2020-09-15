#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Utility;
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
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Body.Part
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedBodyPartComponent))]
    [ComponentReference(typeof(IBodyPart))]
    public class BodyPartComponent : SharedBodyPartComponent
    {
        private readonly Dictionary<int, object> _optionsCache = new Dictionary<int, object>();

        private IBody? _owningBodyCache;

        private int _idHash;

        private IEntity? _surgeonCache;

        private SurgeryData? _surgeryData;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(SurgeryUIKey.Key);

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _surgeryData, "surgeryData", new BiologicalSurgeryData(this));
        }

        public override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUIMessage;
            }

            if (Owner.TryGetComponent(out SpriteComponent? sprite))
            {
                sprite.LayerSetRSI(0, RSIPath);
                sprite.LayerSetState(0, RSIState);

                if (RSIColor.HasValue)
                {
                    sprite.LayerSetColor(0, RSIColor.Value);
                }
            }
        }

        public void AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null)
            {
                return;
            }

            CloseAllSurgeryUIs();
            _optionsCache.Clear();
            _surgeonCache = null;
            _owningBodyCache = null;

            if (eventArgs.Target.TryGetComponent(out BodyComponent? bodyManager))
            {
                SendSlots(eventArgs, bodyManager);
            }
        }

        private void SendSlots(AfterInteractEventArgs eventArgs, BodyComponent body)
        {
            // Create dictionary to send to client (text to be shown : data sent back if selected)
            var toSend = new Dictionary<string, int>();

            // Here we are trying to grab a list of all empty BodySlots adjacent to an existing BodyPart that can be
            // attached to. i.e. an empty left hand slot, connected to an occupied left arm slot would be valid.
            var unoccupiedSlots = body.AllSlots.ToList().Except(body.OccupiedSlots.ToList()).ToList();
            foreach (var slot in unoccupiedSlots)
            {
                if (!body.TryGetSlotType(slot, out var typeResult) ||
                    typeResult != PartType ||
                    !body.TryGetPartConnections(slot, out var parts))
                {
                    continue;
                }

                foreach (var connectedPart in parts)
                {
                    if (!connectedPart.CanAttachPart(this))
                    {
                        continue;
                    }

                    _optionsCache.Add(_idHash, slot);
                    toSend.Add(slot, _idHash++);
                }
            }

            if (_optionsCache.Count > 0)
            {
                OpenSurgeryUI(eventArgs.User.GetComponent<BasicActorComponent>().playerSession);
                BodyPartSlotRequest(eventArgs.User.GetComponent<BasicActorComponent>().playerSession,
                    toSend);
                _surgeonCache = eventArgs.User;
                _owningBodyCache = body;
            }
            else // If surgery cannot be performed, show message saying so.
            {
                eventArgs.Target.PopupMessage(eventArgs.User,
                    Loc.GetString("You see no way to install {0:theName}.", Owner));
            }
        }

        /// <summary>
        ///     Called after the client chooses from a list of possible BodyPartSlots to install the limb on.
        /// </summary>
        private void ReceiveBodyPartSlot(int key)
        {
            if (_surgeonCache == null ||
                !_surgeonCache.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            CloseSurgeryUI(actor.playerSession);

            if (_owningBodyCache == null)
            {
                return;
            }

            // TODO: sanity checks to see whether user is in range, user is still able-bodied, target is still the same, etc etc
            if (!_optionsCache.TryGetValue(key, out var targetObject))
            {
                _owningBodyCache.Owner.PopupMessage(_surgeonCache,
                    Loc.GetString("You see no useful way to attach {0:theName} anymore.", Owner));
            }

            var target = (string) targetObject!;
            string message;

            message = _owningBodyCache.TryAddPart(target, this)
                ? Loc.GetString("You attach {0:theName}.", Owner)
                : Loc.GetString("You can't attach {0:theName}!", Owner);

            _owningBodyCache.Owner.PopupMessage(_surgeonCache, message);
        }

        private void OpenSurgeryUI(IPlayerSession session)
        {
            UserInterface?.Open(session);
        }

        private void BodyPartSlotRequest(IPlayerSession session, Dictionary<string, int> options)
        {
            UserInterface?.SendMessage(new RequestBodyPartSlotSurgeryUIMessage(options), session);
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
                case ReceiveBodyPartSlotSurgeryUIMessage msg:
                    ReceiveBodyPartSlot(msg.SelectedOptionId);
                    break;
            }
        }
    }
}
