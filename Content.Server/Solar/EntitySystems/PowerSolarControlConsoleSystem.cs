using Content.Server.Solar.Components;
using Content.Server.UserInterface;
using Content.Shared.Solar;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

namespace Content.Server.Solar.EntitySystems
{
    /// <summary>
    /// Responsible for updating solar control consoles.
    /// </summary>
    [UsedImplicitly]
    internal sealed class PowerSolarControlConsoleSystem : EntitySystem
    {
        [Dependency] private readonly PowerSolarSystem _powerSolarSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        /// <summary>
        /// Timer used to avoid updating the UI state every frame (which would be overkill)
        /// </summary>
        private float _updateTimer;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SolarControlConsoleComponent, SolarControlConsoleAdjustMessage>(OnUIMessage);
        }

        public override void Update(float frameTime)
        {
            _updateTimer += frameTime;
            if (_updateTimer >= 1)
            {
                _updateTimer -= 1;
                var state = new SolarControlConsoleBoundInterfaceState(_powerSolarSystem.TargetPanelRotation, _powerSolarSystem.TargetPanelVelocity, _powerSolarSystem.TotalPanelPower, _powerSolarSystem.TowardsSun);
                var query = EntityQueryEnumerator<SolarControlConsoleComponent, UserInterfaceComponent>();
                while (query.MoveNext(out var uid, out var _, out var uiComp))
                {
                    _uiSystem.TrySetUiState(uid, SolarControlConsoleUiKey.Key, state, ui: uiComp);
                }
            }
        }
 
        private void OnUIMessage(EntityUid uid, SolarControlConsoleComponent component, SolarControlConsoleAdjustMessage msg)
        {
            if (double.IsFinite(msg.Rotation))
            {
                _powerSolarSystem.TargetPanelRotation = msg.Rotation.Reduced();
            }
            if (double.IsFinite(msg.AngularVelocity))
            {
                var degrees = msg.AngularVelocity.Degrees;
                degrees = Math.Clamp(degrees, -PowerSolarSystem.MaxPanelVelocityDegrees, PowerSolarSystem.MaxPanelVelocityDegrees);
                _powerSolarSystem.TargetPanelVelocity = Angle.FromDegrees(degrees);
            }
        }

    }
}
