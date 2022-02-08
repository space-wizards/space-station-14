using Content.Server.UserInterface;
using Content.Shared.Interaction;
using Content.Shared.Research.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Server.Research.Components
{
    [RegisterComponent]
    public class ResearchClientComponent : SharedResearchClientComponent, IActivate
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        // TODO: Create GUI for changing RD server.
        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(ResearchClientUiKey.Key);

        public bool ConnectedToServer => Server != null;

        [ViewVariables(VVAccess.ReadOnly)]
        public ResearchServerComponent? Server { get; set; }

        public bool RegisterServer(ResearchServerComponent? server)
        {
            var result = server != null && server.RegisterClient(this);
            return result;
        }

        public void UnregisterFromServer()
        {
            Server?.UnregisterClient(this);
        }

        protected override void Initialize()
        {
            base.Initialize();

            // For now it just registers on the first server it can find.
            var servers = _entitySystemManager.GetEntitySystem<ResearchSystem>().Servers;

            if (servers.Count > 0)
                RegisterServer(servers[0]);

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            }
        }

        public void OpenUserInterface(IPlayerSession session)
        {
            UpdateUserInterface();
            UserInterface?.Open(session);
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(eventArgs.User, out ActorComponent? actor))
                return;

            OpenUserInterface(actor.PlayerSession);
        }

        public void UpdateUserInterface()
        {
            UserInterface?.SetState(GetNewUiState());
        }

        private ResearchClientBoundInterfaceState GetNewUiState()
        {
            var rd = _entitySystemManager.GetEntitySystem<ResearchSystem>();

            return new ResearchClientBoundInterfaceState(rd.Servers.Count, rd.GetServerNames(),
                rd.GetServerIds(), ConnectedToServer ? Server!.Id : -1);
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage msg)
        {
            switch (msg.Message)
            {
                case ResearchClientSyncMessage _:
                    UpdateUserInterface();
                    break;

                case ResearchClientServerSelectedMessage selectedMessage:
                    UnregisterFromServer();
                    RegisterServer(_entitySystemManager.GetEntitySystem<ResearchSystem>().GetServerById(selectedMessage.ServerId));
                    UpdateUserInterface();
                    break;

                case ResearchClientServerDeselectedMessage _:
                    UnregisterFromServer();
                    UpdateUserInterface();
                    break;
            }
        }

        /// <inheritdoc />
        protected override void Shutdown()
        {
            base.Shutdown();
            UnregisterFromServer();

        }
    }
}
