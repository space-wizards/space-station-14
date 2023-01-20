using Content.Shared.Alert;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Buckle.Components;

public enum StrapPosition
{
    /// <summary>
    /// (Default) Makes no change to the buckled mob
    /// </summary>
    None = 0,

    /// <summary>
    /// Makes the mob stand up
    /// </summary>
    Stand,

    /// <summary>
    /// Makes the mob lie down
    /// </summary>
    Down
}

[RegisterComponent, NetworkedComponent]
public sealed class StrapComponent : Component
{
    /// <summary>
    /// The change in position to the strapped mob
    /// </summary>
    [DataField("position")]
    public StrapPosition Position { get; set; } = StrapPosition.None;

    /// <summary>
    /// The entity that is currently buckled here
    /// </summary>
    public readonly HashSet<EntityUid> BuckledEntities = new();

    /// <summary>
    /// The distance above which a buckled entity will be automatically unbuckled.
    /// Don't change it unless you really have to
    /// </summary>
    [DataField("maxBuckleDistance", required: false)]
    public float MaxBuckleDistance = 0.1f;

    /// <summary>
    /// Gets and clamps the buckle offset to MaxBuckleDistance
    /// </summary>
    public Vector2 BuckleOffset => Vector2.Clamp(
        BuckleOffsetUnclamped,
        Vector2.One * -MaxBuckleDistance,
        Vector2.One * MaxBuckleDistance);

    /// <summary>
    /// The buckled entity will be offset by this amount from the center of the strap object.
    /// If this offset it too big, it will be clamped to <see cref="MaxBuckleDistance"/>
    /// </summary>
    [DataField("buckleOffset", required: false)]
    [Access(Other = AccessPermissions.ReadWrite)]
    public Vector2 BuckleOffsetUnclamped = Vector2.Zero;


    /// <summary>
    /// The angle in degrees to rotate the player by when they get strapped
    /// </summary>
    [DataField("rotation")]
    public int Rotation { get; set; }

    /// <summary>
    /// The size of the strap which is compared against when buckling entities
    /// </summary>
    [DataField("size")]
    public int Size { get; set; } = 100;

    /// <summary>
    /// If disabled, nothing can be buckled on this object, and it will unbuckle anything that's already buckled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// You can specify the offset the entity will have after unbuckling.
    /// </summary>
    [DataField("unbuckleOffset", required: false)]
    public Vector2 UnbuckleOffset = Vector2.Zero;
    /// <summary>
    /// The sound to be played when a mob is buckled
    /// </summary>
    [DataField("buckleSound")]
    public SoundSpecifier BuckleSound { get; } = new SoundPathSpecifier("/Audio/Effects/buckle.ogg");

    /// <summary>
    /// The sound to be played when a mob is unbuckled
    /// </summary>
    [DataField("unbuckleSound")]
    public SoundSpecifier UnbuckleSound { get; } = new SoundPathSpecifier("/Audio/Effects/unbuckle.ogg");

    /// <summary>
    /// ID of the alert to show when buckled
    /// </summary>
    [DataField("buckledAlertType")]
    public AlertType BuckledAlertType { get; } = AlertType.Buckled;

    /// <summary>
    /// The sum of the sizes of all the buckled entities in this strap
    /// </summary>
    public int OccupiedSize { get; set; }
}

[Serializable, NetSerializable]
public sealed class StrapComponentState : ComponentState
{
    /// <summary>
    /// The change in position that this strap makes to the strapped mob
    /// </summary>
    public StrapPosition Position;

    public float MaxBuckleDistance;
    public Vector2 BuckleOffsetClamped;
    public HashSet<EntityUid> BuckledEntities;

    public StrapComponentState(StrapPosition position, Vector2 offset, HashSet<EntityUid> buckled, float maxBuckleDistance)
    {
        Position = position;
        BuckleOffsetClamped = offset;
        BuckledEntities = buckled;
        MaxBuckleDistance = maxBuckleDistance;
    }
}

[Serializable, NetSerializable]
public enum StrapVisuals : byte
{
    RotationAngle,
    State
}
