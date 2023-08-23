using System.Numerics;
using Content.Shared.Alert;
using Content.Shared.Vehicle;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Buckle.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBuckleSystem), typeof(SharedVehicleSystem))]
public sealed partial class StrapComponent : Component
{
    /// <summary>
    /// The entities that are currently buckled
    /// </summary>
    [ViewVariables] // TODO serialization
    public readonly HashSet<EntityUid> BuckledEntities = new();

    /// <summary>
    /// Entities that this strap accepts and can buckle
    /// If null it accepts any entity
    /// </summary>
    [DataField("allowedEntities")]
    [ViewVariables]
    public EntityWhitelist? AllowedEntities;

    /// <summary>
    /// The change in position to the strapped mob
    /// </summary>
    [DataField("position")]
    [ViewVariables(VVAccess.ReadWrite)]
    public StrapPosition Position = StrapPosition.None;

    /// <summary>
    /// The distance above which a buckled entity will be automatically unbuckled.
    /// Don't change it unless you really have to
    /// </summary>
    /// <remarks>
    /// Dont set this below 0.2 because that causes audio issues with <see cref="SharedBuckleSystem.OnBuckleMove"/>
    /// My guess after testing is that the client sets BuckledTo to the strap in *some* ticks for some reason
    /// whereas the server doesnt, thus the client tries to unbuckle like 15 times because it passes the strap null check
    /// This is why this needs to be above 0.1 to make the InRange check fail in both client and server.
    /// </remarks>
    [DataField("maxBuckleDistance", required: false)]
    [ViewVariables(VVAccess.ReadWrite)]
    public float MaxBuckleDistance = 0.2f;

    /// <summary>
    /// Gets and clamps the buckle offset to MaxBuckleDistance
    /// </summary>
    [ViewVariables]
    public Vector2 BuckleOffset => Vector2.Clamp(
        BuckleOffsetUnclamped,
        Vector2.One * -MaxBuckleDistance,
        Vector2.One * MaxBuckleDistance);

    /// <summary>
    /// The buckled entity will be offset by this amount from the center of the strap object.
    /// If this offset it too big, it will be clamped to <see cref="MaxBuckleDistance"/>
    /// </summary>
    [DataField("buckleOffset", required: false)]
    [ViewVariables(VVAccess.ReadWrite)]
    public Vector2 BuckleOffsetUnclamped = Vector2.Zero;

    /// <summary>
    /// The angle in degrees to rotate the player by when they get strapped
    /// </summary>
    [DataField("rotation")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int Rotation;

    /// <summary>
    /// The size of the strap which is compared against when buckling entities
    /// </summary>
    [DataField("size")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int Size = 100;

    /// <summary>
    /// If disabled, nothing can be buckled on this object, and it will unbuckle anything that's already buckled
    /// </summary>
    [ViewVariables]
    public bool Enabled = true;

    /// <summary>
    /// You can specify the offset the entity will have after unbuckling.
    /// </summary>
    [DataField("unbuckleOffset", required: false)]
    [ViewVariables(VVAccess.ReadWrite)]
    public Vector2 UnbuckleOffset = Vector2.Zero;

    /// <summary>
    /// The sound to be played when a mob is buckled
    /// </summary>
    [DataField("buckleSound")]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier BuckleSound  = new SoundPathSpecifier("/Audio/Effects/buckle.ogg");

    /// <summary>
    /// The sound to be played when a mob is unbuckled
    /// </summary>
    [DataField("unbuckleSound")]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier UnbuckleSound = new SoundPathSpecifier("/Audio/Effects/unbuckle.ogg");

    /// <summary>
    /// ID of the alert to show when buckled
    /// </summary>
    [DataField("buckledAlertType")]
    [ViewVariables(VVAccess.ReadWrite)]
    public AlertType BuckledAlertType = AlertType.Buckled;

    /// <summary>
    /// The sum of the sizes of all the buckled entities in this strap
    /// </summary>
    [ViewVariables]
    public int OccupiedSize;
}

[Serializable, NetSerializable]
public sealed class StrapComponentState : ComponentState
{
    public readonly StrapPosition Position;
    public readonly float MaxBuckleDistance;
    public readonly Vector2 BuckleOffsetClamped;
    public readonly HashSet<NetEntity> BuckledEntities;
    public readonly int OccupiedSize;

    public StrapComponentState(StrapPosition position, Vector2 offset, HashSet<NetEntity> buckled,
        float maxBuckleDistance, int occupiedSize)
    {
        Position = position;
        BuckleOffsetClamped = offset;
        BuckledEntities = buckled;
        MaxBuckleDistance = maxBuckleDistance;
        OccupiedSize = occupiedSize;
    }
}

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

[Serializable, NetSerializable]
public enum StrapVisuals : byte
{
    RotationAngle,
    State
}
