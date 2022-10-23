using Content.Server.UserInterface;
using Content.Shared.Body.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Body.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedBodyScannerComponent))]
    public sealed class BodyScannerComponent : SharedBodyScannerComponent
    {
        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(BodyScannerUiKey.Key);
        protected override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponentWarn<ServerUserInterfaceComponent>();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            }
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg) { }

        /// <summary>
        ///     Copy BodyTemplate and BodyPart data into a common data class that the client can read.
        /// </summary>
        private BodyScannerUIState InterfaceState(BodyComponent body)
        {
            return new(body.Owner);
        }
    }
}
