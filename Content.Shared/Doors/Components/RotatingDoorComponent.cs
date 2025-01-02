using Content.Shared.Doors.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Doors.Components;

/// <summary>
/// A door that uses two discrete fixtures to model a "rotating" door.
/// </summary>
[RegisterComponent, NetworkedComponent /*, AutoGenerateComponentState*/]
[Access(typeof(SharedDoorSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
public sealed partial class RotatingDoorComponent : Component
{
    [DataField]
    public string InnerFixtureName;

    [DataField]
    public string OuterFixtureName;

    [DataField]
    public PolygonShape InnerDoor;

    [DataField]
    public PolygonShape OuterDoor;

    [DataField]
    public float Density = 1500f;

    /// <summary>
    /// Bitmask of the collision layers the doors are a part of.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite),
     DataField("layer", customTypeSerializer: typeof(FlagSerializer<CollisionLayer>)),
     Access(typeof(SharedPhysicsSystem),
         Friend = AccessPermissions.ReadWriteExecute,
         Other = AccessPermissions.Read)]
    public int CollisionLayer;

    /// <summary>
    ///  Bitmask of the layers this door collides with.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("mask", customTypeSerializer: typeof(FlagSerializer<CollisionMask>)),
     Access(typeof(SharedPhysicsSystem),
         Friend = AccessPermissions.ReadWriteExecute,
         Other = AccessPermissions.Read)]
    public int CollisionMask;
}
