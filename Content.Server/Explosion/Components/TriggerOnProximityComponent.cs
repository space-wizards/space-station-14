using Content.Server.Explosion.EntitySystems;
using Content.Shared.Explosion;
using Content.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Explosion.Components
{

    /// <summary>
    /// Raises a <see cref="TriggerEvent"/> whenever an entity collides with a fixture attached to the owner of this component.
    /// </summary>
    [RegisterComponent]
    public sealed partial class TriggerOnProximityComponent : SharedTriggerOnProximityComponent
    {
        public const string FixtureID = "trigger-on-proximity-fixture";

        [ViewVariables]
        public readonly Dictionary<EntityUid, PhysicsComponent> Colliding = new();

        /// <summary>
        /// What is the shape of the proximity fixture?
        /// </summary>
        [ViewVariables]
        [DataField("shape")]
        public IPhysShape Shape = new PhysShapeCircle(2f);

        /// <summary>
        /// How long the the proximity trigger animation plays for.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("animationDuration")]
        public TimeSpan AnimationDuration = TimeSpan.FromSeconds(0.6f);

        /// <summary>
        /// Whether the entity needs to be anchored for the proximity to work.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("requiresAnchored")]
        public bool RequiresAnchored = true;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("enabled")]
        public bool Enabled = true;

        /// <summary>
        /// The minimum delay between repeating triggers.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("cooldown")]
        public TimeSpan Cooldown = TimeSpan.FromSeconds(5);

        /// <summary>
        /// When can the trigger run again?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("nextTrigger", customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan NextTrigger = TimeSpan.Zero;

        /// <summary>
        /// When will the visual state be updated again after activation?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("nextVisualUpdate", customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan NextVisualUpdate = TimeSpan.Zero;

        /// <summary>
        /// What speed should the other object be moving at to trigger the proximity fixture?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("triggerSpeed")]
        public float TriggerSpeed = 3.5f;

        /// <summary>
        /// If this proximity is triggered should we continually repeat it?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("repeating")]
        public bool Repeating = true;

        /// <summary>
        /// What layer is the trigger fixture on?
        /// </summary>
        [ViewVariables]
        [DataField("layer", customTypeSerializer: typeof(FlagSerializer<CollisionLayer>))]
        public int Layer = (int) (CollisionGroup.MidImpassable | CollisionGroup.LowImpassable | CollisionGroup.HighImpassable);
    }
}
