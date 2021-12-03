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
    public class BodyScannerComponent : SharedBodyScannerComponent, IActivate
    {
        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(BodyScannerUiKey.Key);

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(eventArgs.User.Uid, out ActorComponent? actor))
            {
                return;
            }

            var session = actor.PlayerSession;

            if (session.AttachedEntity == null)
            {
                return;
            }

            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(session.AttachedEntity.Uid, out SharedBodyComponent? body))
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
            return new(body.Owner.Uid);
        }
    }
}
