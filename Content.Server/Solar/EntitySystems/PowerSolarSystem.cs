using System.Linq;
using Content.Server.Power.Components;
using Content.Server.Solar.Components;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Physics;
using JetBrains.Annotations;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Server.Solar.EntitySystems
{
    /// <summary>
    ///     Responsible for maintaining the solar-panel sun angle and updating <see cref='SolarPanelComponent'/> coverage.
    /// </summary>
    [UsedImplicitly]
    internal sealed partial class PowerSolarSystem : EntitySystem
    {
        [Dependency] private IRobustRandom _robustRandom = default!;
        [Dependency] private SharedPhysicsSystem _physicsSystem = default!;
        [Dependency] private SharedTransformSystem _transformSystem = default!;

        /// <summary>
        /// Maximum panel angular velocity range - used to stop people rotating panels fast enough that the lag prevention becomes noticable
        /// </summary>
        public const float MaxPanelVelocityDegrees = 1f;

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

        /// <summary>
        /// Queue of panels to update each cycle.
        /// </summary>
        private readonly Queue<Entity<SolarPanelComponent>> _updateQueue = new();

        public override void Initialize()
        {
            RandomizeSun();
        }

        [SubscribeLocalEvent]
        public void Reset(RoundRestartCleanupEvent ev)
        {
            RandomizeSun();
            TargetPanelRotation = Angle.Zero;
            TargetPanelVelocity = Angle.Zero;
            TotalPanelPower = 0;
        }

        private void RandomizeSun()
        {
            // Initialize the sun to something random
            TowardsSun = Math.Tau * _robustRandom.NextDouble();
            SunAngularVelocity = Angle.FromDegrees(0.1 + ((_robustRandom.NextDouble() - 0.5) * 0.05));
        }

        [SubscribeLocalEvent]
        private void OnMapInit(EntityUid uid, SolarPanelComponent component, MapInitEvent args)
        {
            UpdateSupply(uid, component);
        }

        [SubscribeLocalEvent]
        private void OnExamined(Entity<SolarPanelComponent> ent, ref ExaminedEvent args)
        {
            var xform = Transform(ent);
            if (!xform.Anchored)
                return;

            if (ent.Comp.Coverage is 0)
            {
                if (IsPanelOccluded(ent.Owner, xform))
                    args.PushMarkup(Loc.GetString("solarpanel-examine-occluded"));
                else
                    args.PushMarkup(Loc.GetString("solarpanel-examine-unaligned"));
            }
            else
            {
                // Consider aligned within 30⁰
                if (ent.Comp.Coverage > Math.Cos(Angle.FromDegrees(30)))
                    args.PushMarkup(Loc.GetString("solarpanel-examine-aligned"));
                else
                    args.PushMarkup(Loc.GetString("solarpanel-examine-misaligned"));
            }
        }

        public override void Update(float frameTime)
        {
            TowardsSun += SunAngularVelocity * frameTime;
            TowardsSun = TowardsSun.Reduced();

            TargetPanelRotation += TargetPanelVelocity * frameTime;
            TargetPanelRotation = TargetPanelRotation.Reduced();

            if (_updateQueue.Count > 0)
            {
                var panel = _updateQueue.Dequeue();
                if (panel.Comp.Running)
                    UpdatePanelCoverage(panel);
            }
            else
            {
                TotalPanelPower = 0;

                var query = EntityQueryEnumerator<SolarPanelComponent, TransformComponent>();
                while (query.MoveNext(out var uid, out var panel, out var xform))
                {
                    TotalPanelPower += panel.MaxSupply * panel.Coverage;
                    _transformSystem.SetWorldRotation(xform, TargetPanelRotation);
                    _updateQueue.Enqueue((uid, panel));
                }
            }
        }

        private void UpdatePanelCoverage(Entity<SolarPanelComponent> panel)
        {
            var entity = panel.Owner;
            var xform = Comp<TransformComponent>(entity);

            var coverage = GetSolarCoverage(xform);

            if (coverage > 0)
            {
                var isOccluded = IsPanelOccluded(entity, xform);
                if (isOccluded)
                    coverage = 0;
            }

            // Total coverage calculated; apply it to the panel.
            panel.Comp.Coverage = coverage;
            UpdateSupply(panel, panel);
        }

        /// <summary>
        /// Gets the coverage of the solar panel.
        /// </summary>
        private float GetSolarCoverage(TransformComponent xform)
        {
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
            var panelRelativeToSun = _transformSystem.GetWorldRotation(xform) - TowardsSun;
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
            var coverage = (float)Math.Max(0, Math.Cos(panelRelativeToSun));
            return coverage;
        }

        /// <summary>
        /// Determine if the solar panel is occluded.
        /// </summary>
        private bool IsPanelOccluded(EntityUid entity, TransformComponent xform)
        {
            var ray = new CollisionRay(_transformSystem.GetWorldPosition(xform), TowardsSun.ToWorldVec(), (int)CollisionGroup.Opaque);
            var rayCastResults = _physicsSystem.IntersectRayWithPredicate(
                xform.MapID,
                ray,
                SunOcclusionCheckDistance,
                e => !xform.Anchored || e == entity);
            return rayCastResults.Any();
        }

        public void UpdateSupply(
            EntityUid uid,
            SolarPanelComponent? solar = null,
            PowerSupplierComponent? supplier = null)
        {
            if (!Resolve(uid, ref solar, ref supplier, false))
                return;

            supplier.MaxSupply = (int) (solar.MaxSupply * solar.Coverage);
        }
    }
}
