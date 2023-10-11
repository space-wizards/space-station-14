using System.Numerics;
using Content.Shared.Alert;
using Content.Shared.Vehicle;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Buckle.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedBuckleSystem), typeof(SharedVehicleSystem))]
public sealed partial class StrapComponent : Component
{
    /// <summary>
    /// The entities that are currently buckled
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public HashSet<EntityUid> BuckledEntities = new();

    /// <summary>
    /// Entities that this strap accepts and can buckle
    /// If null it accepts any entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? AllowedEntities;

    /// <summary>
    /// The angle in degrees to rotate the player by when they get strapped
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Rotation;

    /// <summary>
    /// The size of the strap which is compared against when buckling entities
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Size = 100;

    /// <summary>
    /// If disabled, nothing can be buckled on this object, and it will unbuckle anything that's already buckled
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// The sound to be played when a mob is buckled
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier BuckleSound  = new SoundPathSpecifier("/Audio/Effects/buckle.ogg");

    /// <summary>
    /// The sound to be played when a mob is unbuckled
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier UnbuckleSound = new SoundPathSpecifier("/Audio/Effects/unbuckle.ogg");

    /// <summary>
    /// ID of the alert to show when buckled
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public AlertType BuckledAlertType = AlertType.Buckled;

    /// <summary>
    /// The sum of the sizes of all the buckled entities in this strap
    /// </summary>
    [DataField, AutoNetworkedField]
    public int OccupiedSize;
}

[Serializable, NetSerializable]
public enum StrapVisuals : byte
{
    RotationAngle,
    State
}
