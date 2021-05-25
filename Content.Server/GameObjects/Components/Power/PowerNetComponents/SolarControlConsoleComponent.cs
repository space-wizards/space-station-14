#nullable enable
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Power;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power.PowerNetComponents
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class SolarControlConsoleComponent : BaseComputerUserInterfaceComponent
    {
        public override string Name => "SolarControlConsole";

        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        private PowerSolarSystem _powerSolarSystem = default!;

        public SolarControlConsoleComponent() : base(SolarControlConsoleUiKey.Key) { }

        public override void Initialize()
        {
            base.Initialize();
            _powerSolarSystem = _entitySystemManager.GetEntitySystem<PowerSolarSystem>();
        }

        public void UpdateUIState()
        {
            UserInterface?.SetState(new SolarControlConsoleBoundInterfaceState(_powerSolarSystem.TargetPanelRotation, _powerSolarSystem.TargetPanelVelocity, _powerSolarSystem.TotalPanelPower, _powerSolarSystem.TowardsSun));
        }

        protected override void OnReceiveUserInterfaceMessage(ServerBoundUserInterfaceMessage obj)
        {
            switch (obj.Message)
            {
                case SolarControlConsoleAdjustMessage msg:
                    if (double.IsFinite(msg.Rotation))
                    {
                        _powerSolarSystem.TargetPanelRotation = msg.Rotation.Reduced();
                    }
                    if (double.IsFinite(msg.AngularVelocity))
                    {
                        _powerSolarSystem.TargetPanelVelocity = msg.AngularVelocity.Reduced();
                    }
                    break;
            }
        }
    }
}
