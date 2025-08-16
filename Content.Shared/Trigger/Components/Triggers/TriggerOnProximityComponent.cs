using Content.Shared.Physics;
using Content.Shared.Trigger.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers whenever an entity collides with a fixture attached to the owner of this component.
/// The user is the entity that collided with the fixture.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class TriggerOnProximityComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// The keys that, upon being triggered, will enable the proximity trigger.
    /// If this shares a key with <see cref="DisablingKeysIn"/>, this trigger
    /// will be enabled when that key gets triggered.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> EnablingKeysIn = new();

    /// <summary>
    /// The keys that, upon being triggered, will disable the proximity trigger.
    /// If this shares a key with <see cref="EnablingKeysIn"/>, this proximity
    /// will be enabled when that key gets triggered.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> DisablingKeysIn = new();

    /// <summary>
    /// The keys that, upon being triggered, will enable the proximity trigger
    /// if it is disabled, and disable it if it is enabled. Processes after
    /// <see cref="DisablingKeysIn"/> and <see cref="EnablingKeysIn"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> TogglingKeysIn = new();

    /// <summary>
    /// The ID if the fixture that is observed for collisions.
    /// </summary>
    public const string FixtureID = "trigger-on-proximity-fixture";

    /// <summary>
    /// Currently colliding entities.
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<EntityUid, PhysicsComponent> Colliding = new();

    /// <summary>
    /// What is the shape of the proximity fixture?
    /// </summary>
    [ViewVariables]
    [DataField]
    public IPhysShape Shape = new PhysShapeCircle(2f);

    /// <summary>
    /// How long the the proximity trigger animation plays for.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan AnimationDuration = TimeSpan.FromSeconds(0.6f);

    /// <summary>
    /// Whether an unoccluded line of sight between the proximity trigger and any
    /// colliding objects is required for it to be triggered.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RequiresLineOfSight = true;

    /// <summary>
    /// Should an examiner be told whether this proximity trigger is enabled
    /// or not?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Examinable = false;

    /// <summary>
    /// Whether the entity needs to be anchored for the proximity to work.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RequiresAnchored = true;

    /// <summary>
    /// Whether the proximity trigger is currently enabled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// The minimum delay between repeating triggers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(5);

    /// <summary>
    /// When can the trigger run again?
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextTrigger = TimeSpan.Zero;

    /// <summary>
    /// When will the visual state be updated again after activation?
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextVisualUpdate = TimeSpan.Zero;

    /// <summary>
    /// What speed should the other object be moving at to trigger the proximity fixture?
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TriggerSpeed = 3.5f;

    /// <summary>
    /// If this proximity is triggered should we continually repeat it?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Repeating = true;

    /// <summary>
    /// What layer is the trigger fixture on?
    /// </summary>
    [DataField(customTypeSerializer: typeof(FlagSerializer<CollisionLayer>))]
    public int Layer = (int)(CollisionGroup.MidImpassable | CollisionGroup.LowImpassable | CollisionGroup.HighImpassable);
}
