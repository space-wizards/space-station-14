using System;
using System.Linq;
using Content.Server.Solar.Components;
using Content.Shared.Physics;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Solar.EntitySystems
{
    /// <summary>
    ///     Responsible for maintaining the solar-panel sun angle and updating <see cref='SolarPanelComponent'/> coverage.
    /// </summary>
    [UsedImplicitly]
    internal sealed class PowerSolarSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        /// <summary>
        /// The current sun angle.
        /// </summary>
        public Angle TowardsSun = Angle.Zero;

        /// <summary>
        /// The current sun angular velocity. (This is changed in Initialize)
        /// </summary>
        public Angle SunAngularVelocity = Angle.Zero;

        /// <summary>
        /// The distance before the sun is considered to have been 'visible anyway'.
        /// This value, like the occlusion semantics, is borrowed from all the other SS13 stations with solars.
        /// </summary>
        public float SunOcclusionCheckDistance = 20;

        /// <summary>
        /// This is the per-second value used to reduce solar panel coverage updates
        /// (and the resulting occlusion raycasts)
        /// to within sane boundaries.
        /// Keep in mind, this is not exact, as the random interval is also applied.
        /// </summary>
        public TimeSpan SolarCoverageUpdateInterval = TimeSpan.FromSeconds(0.5);

        /// <summary>
        /// A random interval used to stagger solar coverage updates reliably.
        /// </summary>
        public TimeSpan SolarCoverageUpdateRandomInterval = TimeSpan.FromSeconds(0.5);

        /// <summary>
        /// TODO: *Should be moved into the solar tracker when powernet allows for it.*
        /// The current target panel rotation.
        /// </summary>
        public Angle TargetPanelRotation = Angle.Zero;

        /// <summary>
        /// TODO: *Should be moved into the solar tracker when powernet allows for it.*
        /// The current target panel velocity.
        /// </summary>
        public Angle TargetPanelVelocity = Angle.Zero;

        /// <summary>
        /// TODO: *Should be moved into the solar tracker when powernet allows for it.*
        /// Last update of total panel power.
        /// </summary>
        public float TotalPanelPower = 0;

        public override void Initialize()
        {
            // Initialize the sun to something random
            TowardsSun = MathHelper.TwoPi * _robustRandom.NextDouble();
            SunAngularVelocity = Angle.FromDegrees(0.1 + ((_robustRandom.NextDouble() - 0.5) * 0.05));
        }

        public override void Update(float frameTime)
        {
            TowardsSun += SunAngularVelocity * frameTime;
            TowardsSun = TowardsSun.Reduced();

            TargetPanelRotation += TargetPanelVelocity * frameTime;
            TargetPanelRotation = TargetPanelRotation.Reduced();

            TotalPanelPower = 0;

            foreach (var panel in EntityManager.EntityQuery<SolarPanelComponent>())
            {
                // There's supposed to be rotational logic here, but that implies putting it somewhere.
                panel.Owner.Transform.WorldRotation = TargetPanelRotation;

                if (panel.TimeOfNextCoverageUpdate < _gameTiming.CurTime)
                {
                    // Setup the next coverage check.
                    TimeSpan future = SolarCoverageUpdateInterval + (SolarCoverageUpdateRandomInterval * _robustRandom.NextDouble());
                    panel.TimeOfNextCoverageUpdate = _gameTiming.CurTime + future;
                    UpdatePanelCoverage(panel);
                }
                TotalPanelPower += panel.Coverage * panel.MaxSupply;
            }
        }

        private void UpdatePanelCoverage(SolarPanelComponent panel) {
            IEntity entity = panel.Owner;

            // So apparently, and yes, I *did* only find this out later,
            // this is just a really fancy way of saying "Lambert's law of cosines".
            // ...I still think this explaination makes more sense.

            // In the 'sunRelative' coordinate system:
            // the sun is considered to be an infinite distance directly up.
            // this is the rotation of the panel relative to that.
            // directly upwards (theta = 0) = coverage 1
            // left/right 90 degrees (abs(theta) = (pi / 2)) = coverage 0
            // directly downwards (abs(theta) = pi) = coverage -1
            // as TowardsSun + = CCW,
            // panelRelativeToSun should - = CW
            var panelRelativeToSun = entity.Transform.WorldRotation - TowardsSun;
            // essentially, given cos = X & sin = Y & Y is 'downwards',
            // then for the first 90 degrees of rotation in either direction,
            // this plots the lower-right quadrant of a circle.
            // now basically assume a line going from the negated X/Y to there,
            // and that's the hypothetical solar panel.
            //
            // since, again, the sun is considered to be an infinite distance upwards,
            // this essentially means Cos(panelRelativeToSun) is half of the cross-section,
            // and since the full cross-section has a max of 2, effectively-halving it is fine.
            //
            // as for when it goes negative, it only does that when (abs(theta) > pi)
            // and that's expected behavior.
            float coverage = (float)Math.Max(0, Math.Cos(panelRelativeToSun));

            if (coverage > 0)
            {
                // Determine if the solar panel is occluded, and zero out coverage if so.
                // FIXME: The "Opaque" collision group doesn't seem to work right now.
                var ray = new CollisionRay(entity.Transform.WorldPosition, TowardsSun.ToWorldVec(), (int) CollisionGroup.Opaque);
                var rayCastResults = Get<SharedPhysicsSystem>().IntersectRay(entity.Transform.MapID, ray, SunOcclusionCheckDistance, entity);
                if (rayCastResults.Any())
                    coverage = 0;
            }

            // Total coverage calculated; apply it to the panel.
            panel.Coverage = coverage;
        }
    }
}
