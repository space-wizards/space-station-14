#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Server.Body;
using Content.Server.Utility;
using Content.Shared.Body.Surgery;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Body
{
    /// <summary>
    ///     Component representing a dropped, tangible <see cref="BodyPart"/> entity.
    /// </summary>
    [RegisterComponent]
    public class DroppedBodyPartComponent : Component, IAfterInteract, IBodyPartContainer
    {
        [Dependency] private readonly ISharedNotifyManager _sharedNotifyManager = default!;

        private readonly Dictionary<int, object> _optionsCache = new Dictionary<int, object>();
        private BodyManagerComponent? _bodyManagerComponentCache;
        private int _idHash;
        private IEntity? _performerCache;

        public sealed override string Name => "DroppedBodyPart";

        [ViewVariables] public BodyPart ContainedBodyPart { get; private set; } = default!;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(GenericSurgeryUiKey.Key);

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

            if (eventArgs.Target.TryGetComponent(out BodyManagerComponent? bodyManager))
            {
                SendBodySlotListToUser(eventArgs, bodyManager);
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

        public void TransferBodyPartData(BodyPart data)
        {
            ContainedBodyPart = data;
            Owner.Name = Loc.GetString(ContainedBodyPart.Name);

            if (Owner.TryGetComponent(out SpriteComponent? component))
            {
                component.LayerSetRSI(0, data.RSIPath);
                component.LayerSetState(0, data.RSIState);

                if (data.RSIColor.HasValue)
                {
                    component.LayerSetColor(0, data.RSIColor.Value);
                }
            }
        }

        private void SendBodySlotListToUser(AfterInteractEventArgs eventArgs, BodyManagerComponent bodyManager)
        {
            // Create dictionary to send to client (text to be shown : data sent back if selected)
            var toSend = new Dictionary<string, int>();

            // Here we are trying to grab a list of all empty BodySlots adjacent to an existing BodyPart that can be
            // attached to. i.e. an empty left hand slot, connected to an occupied left arm slot would be valid.
            var unoccupiedSlots = bodyManager.AllSlots.ToList().Except(bodyManager.OccupiedSlots.ToList()).ToList();
            foreach (var slot in unoccupiedSlots)
            {
                if (!bodyManager.TryGetSlotType(slot, out var typeResult) ||
                    typeResult != ContainedBodyPart?.PartType ||
                    !bodyManager.TryGetBodyPartConnections(slot, out var parts))
                {
                    continue;
                }

                foreach (var connectedPart in parts)
                {
                    if (!connectedPart.CanAttachBodyPart(ContainedBodyPart))
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
                UpdateSurgeryUIBodyPartSlotRequest(eventArgs.User.GetComponent<BasicActorComponent>().playerSession,
                    toSend);
                _performerCache = eventArgs.User;
                _bodyManagerComponentCache = bodyManager;
            }
            else // If surgery cannot be performed, show message saying so.
            {
                _sharedNotifyManager.PopupMessage(eventArgs.Target, eventArgs.User,
                    Loc.GetString("You see no way to install {0:theName}.", Owner));
            }
        }

        /// <summary>
        ///     Called after the client chooses from a list of possible BodyPartSlots to install the limb on.
        /// </summary>
        private void HandleReceiveBodyPartSlot(int key)
        {
            if (_performerCache == null ||
                !_performerCache.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            CloseSurgeryUI(actor.playerSession);

            if (_bodyManagerComponentCache == null)
            {
                return;
            }

            // TODO: sanity checks to see whether user is in range, user is still able-bodied, target is still the same, etc etc
            if (!_optionsCache.TryGetValue(key, out var targetObject))
            {
                _sharedNotifyManager.PopupMessage(_bodyManagerComponentCache.Owner, _performerCache,
                    Loc.GetString("You see no useful way to attach {0:theName} anymore.", Owner));
            }

            var target = (string) targetObject!;
            string message;

            if (_bodyManagerComponentCache.InstallDroppedBodyPart(this, target))
            {
                message = Loc.GetString("You attach {0:theName}.", ContainedBodyPart);
            }
            else
            {
                message = Loc.GetString("You can't attach it!");
            }

            _sharedNotifyManager.PopupMessage(
                _bodyManagerComponentCache.Owner,
                _performerCache,
                message);
        }

        private void OpenSurgeryUI(IPlayerSession session)
        {
            UserInterface?.Open(session);
        }

        private void UpdateSurgeryUIBodyPartSlotRequest(IPlayerSession session, Dictionary<string, int> options)
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

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage message)
        {
            switch (message.Message)
            {
                case ReceiveBodyPartSlotSurgeryUIMessage msg:
                    HandleReceiveBodyPartSlot(msg.SelectedOptionId);
                    break;
            }
        }
    }
}
