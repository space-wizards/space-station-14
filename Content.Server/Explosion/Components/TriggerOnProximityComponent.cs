using Content.Server.Explosion.EntitySystems;
using Content.Shared.Explosion;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;

namespace Content.Server.Explosion.Components
{

    /// <summary>
    /// Raises a <see cref="TriggerEvent"/> whenever an entity collides with a fixture attached to the owner of this component.
    /// </summary>
    [RegisterComponent]
    public sealed class TriggerOnProximityComponent : SharedTriggerOnProximityComponent
    {
        public const string FixtureID  = "trigger-on-proximity-fixture";

        public readonly HashSet<PhysicsComponent> Colliding = new();

        [DataField("shape", required: true)]
        public IPhysShape Shape { get; set; } = new PhysShapeCircle(2f);

        /// <summary>
        /// How long the the proximity trigger animation plays for.
        /// </summary>
        [DataField("animationDuration")]
        public float AnimationDuration = 0.3f;

        /// <summary>
        /// Whether the entity needs to be anchored for the proximity to work.
        /// </summary>
        [DataField("requiresAnchored")]
        public bool RequiresAnchored { get; set; } = true;

        [DataField("enabled")]
        public bool Enabled = true;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("cooldown")]
        public float Cooldown { get; set; } = 5f;

        /// <summary>
        /// How much cooldown has elapsed (if relevant).
        /// </summary>
        [DataField("accumulator")]
        public float Accumulator = 0f;

        /// <summary>
        /// What speed should the other object be moving at to trigger the proximity fixture?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("triggerSpeed")]
        public float TriggerSpeed { get; set; } = 3.5f;

        /// <summary>
        /// If this proximity is triggered should we continually repeat it?
        /// </summary>
        [DataField("repeating")]
        internal bool Repeating = true;
    }
}
