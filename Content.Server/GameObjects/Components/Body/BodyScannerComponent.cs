#nullable enable
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Scanner;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Body
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(SharedBodyScannerComponent))]
    public class BodyScannerComponent : SharedBodyScannerComponent, IActivate
    {
        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(BodyScannerUiKey.Key);

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            var session = actor.playerSession;

            if (session.AttachedEntity == null)
            {
                return;
            }

            if (session.AttachedEntity.TryGetComponent(out IBody? body))
            {
                var state = InterfaceState(body);
                UserInterface?.SetState(state);
            }

            UserInterface?.Open(session);
        }

        public override void Initialize()
        {
            base.Initialize();

            if (UserInterface == null)
            {
                Logger.Warning($"Entity {Owner} at {Owner.Transform.MapPosition} doesn't have a {nameof(ServerUserInterfaceComponent)}");
            }
            else
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            }
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg) { }

        /// <summary>
        ///     Copy BodyTemplate and BodyPart data into a common data class that the client can read.
        /// </summary>
        private BodyScannerUIState InterfaceState(IBody body)
        {
            return new BodyScannerUIState(body.Owner.Uid);
        }
    }
}
