using Content.Server.UserInterface;
using Content.Shared.Body.Components;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(SharedBodyScannerComponent))]
    public sealed class BodyScannerComponent : SharedBodyScannerComponent, IActivate
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(BodyScannerUiKey.Key);

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!_entMan.TryGetComponent(eventArgs.User, out ActorComponent? actor))
            {
                return;
            }

            var session = actor.PlayerSession;

            if (session.AttachedEntity == default)
            {
                return;
            }

            if (_entMan.TryGetComponent(session.AttachedEntity, out SharedBodyComponent? body))
            {
                var state = InterfaceState(body);
                UserInterface?.SetState(state);
            }

            UserInterface?.Open(session);
        }

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
        private BodyScannerUIState InterfaceState(SharedBodyComponent body)
        {
            return new(body.Owner);
        }
    }
}
