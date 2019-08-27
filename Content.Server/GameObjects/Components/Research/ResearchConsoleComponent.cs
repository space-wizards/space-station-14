using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Research;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Log;

namespace Content.Server.GameObjects.Components.Research
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class ResearchConsoleComponent : SharedResearchConsoleComponent, IActivate
    {
        private BoundUserInterface _userInterface;
        private ResearchClientComponent _client;
        private bool _uiDirty = true;
        public override void Initialize()
        {
            base.Initialize();
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>().GetBoundUserInterface(ResearchConsoleUiKey.Key);
            _userInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            _client = Owner.GetComponent<ResearchClientComponent>();
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage message)
        {
            switch (message.Message)
            {
                case ConsoleUnlockTechnology msg:
                    break;
            }
        }

        public void UpdateUserInterface()
        {
            _userInterface.SetState(GetNewUiState());
        }

        private ResearchConsoleBoundInterfaceState GetNewUiState()
        {
            var points = _client.ConnectedToServer ? _client.Server.Point : 0;
            var pointsPerSecond = _client.ConnectedToServer ? _client.Server.PointsPerSecond : 0;

            return new ResearchConsoleBoundInterfaceState(points, pointsPerSecond);
        }

        public void Update(float frameTime)
        {
            if (_uiDirty)
            {
                _uiDirty = false;
                UpdateUserInterface();
            }
        }

        public void OpenUserInterface(IPlayerSession session)
        {
            _userInterface.Open(session);
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent actor))
                return;

            OpenUserInterface(actor.playerSession);
            return;
        }
    }
}
