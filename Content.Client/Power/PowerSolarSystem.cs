using Content.Shared.Atmos.Piping;
using Content.Shared.Solar;
using Content.Shared.VendingMachines;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client.Power
{
    [UsedImplicitly]
    public sealed class PowerSolarSystem : EntitySystem
    {
        // This is used for client-side prediction of the panel rotation.
        // This makes the display feel a lot smoother.
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        /// <summary>
        /// Timestamp of the last update. (used for angle prediction)
        /// </summary>
        private TimeSpan _lastUpdate;

        /// <summary>
        /// The last update's solar panel angle.
        /// </summary>
        public Angle PanelAngle;

        /// <summary>
        /// The last update's solar panel angular velocity.
        /// </summary>
        public Angle PanelAngularVelocity;

        /// <summary>
        /// The predicted solar panel angle from the last update.
        /// </summary>
        public Angle PredictedPanelRotation => PanelAngle + (PanelAngularVelocity * ((_gameTiming.CurTime - _lastUpdate).TotalSeconds));

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<PowerSolarSystemSyncMessage>(OnPowerSolarSystemSyncMessage);
        }
        private void OnPowerSolarSystemSyncMessage(PowerSolarSystemSyncMessage message, EntitySessionEventArgs eventArgs)
        {
            PanelAngle = message.Angle;
            PanelAngularVelocity = message.AngularVelocity;
            _lastUpdate = _gameTiming.CurTime;
        }

        public override void Update(float frameTime)
        {
            Angle rot = PredictedPanelRotation;
            foreach (var (panel, sprite) in EntityManager.EntityQuery<SolarPanelComponent, SpriteComponent>())
            {
                sprite.Rotation = rot;
            }
        }
    }
}
