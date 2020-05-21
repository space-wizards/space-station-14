using Content.Server.GameObjects.Components.Power;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Maths;
using System;

namespace Content.Server.GameObjects.EntitySystems
{
    /// <summary>
    ///     Responsible for maintaining the solar-panel sun angle and updating <see cref='SolarPanelComponent'/> coverage.
    /// </summary>
    [UsedImplicitly]
    public class PowerSolarSystem: EntitySystem
    {
        /// <summary>
        /// The current sun angle.
        /// </summary>
        public Angle TowardsSun = Angle.South;

        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery(typeof(SolarPanelComponent));
        }

        public override void Update(float frameTime)
        {
            TowardsSun += Angle.FromDegrees(frameTime / 10);
            TowardsSun = TowardsSun.Reduced();
            foreach (var entity in RelevantEntities)
            {
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
                float coverage = (float) Math.Max(0, Math.Cos(panelRelativeToSun));

                // Would determine occlusion, but that requires raytraces.
                // And I'm not sure where those are in the codebase.
                // Luckily, auto-rotation isn't in yet, so it won't matter anyway.

                // Total coverage calculated; apply it to the panel.
                var panel = entity.GetComponent<SolarPanelComponent>();
                panel.Coverage = coverage;
            }
        }
    }
}
