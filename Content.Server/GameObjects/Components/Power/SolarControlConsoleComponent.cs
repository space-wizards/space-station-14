using Content.Server.GameObjects.Components.Power;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameTicking;
using Content.Shared.GameObjects.Components.Power;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.Components.Power
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class SolarControlConsoleComponent : SharedSolarControlConsoleComponent, IActivate
    {
#pragma warning disable 649
        [Dependency] private IEntitySystemManager _entitySystemManager;
#pragma warning restore 649

        private BoundUserInterface _userInterface;
        private PowerDeviceComponent _powerDevice;
        private PowerSolarSystem _powerSolarSystem;
        private bool Powered => _powerDevice.Powered;

        public override void Initialize()
        {
            base.Initialize();

            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>().GetBoundUserInterface(SolarControlConsoleUiKey.Key);
            _userInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
            _powerDevice = Owner.GetComponent<PowerDeviceComponent>();
            _powerSolarSystem = _entitySystemManager.GetEntitySystem<PowerSolarSystem>();
        }

        private void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent actor))
            {
                return;
            }

            if (!Powered)
            {
                return;
            }

            _userInterface.Open(actor.playerSession);

            // needs to be on-update or something
            _userInterface.SetState(new SolarControlConsoleBoundInterfaceState());
        }
    }
}
