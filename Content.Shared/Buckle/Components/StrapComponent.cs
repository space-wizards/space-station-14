using System.Numerics;
using Content.Shared.Alert;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Buckle.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedBuckleSystem))]
public sealed partial class StrapComponent : Component
{
    /// <summary>
    /// The entities that are currently buckled to this strap.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> BuckledEntities = new();

    /// <summary>
    /// Entities that this strap accepts and can buckle
    /// If null it accepts any entity
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Entities that this strap does not accept and cannot buckle.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// The change in position to the strapped mob
    /// </summary>
    [DataField, AutoNetworkedField]
    public StrapPosition Position = StrapPosition.None;

    /// <summary>
    /// The buckled entity will be offset by this amount from the center of the strap object.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 BuckleOffset = Vector2.Zero;

    /// <summary>
    /// The angle to rotate the player by when they get strapped
    /// </summary>
    [DataField]
    public Angle Rotation;

    /// <summary>
    /// The size of the strap which is compared against when buckling entities
    /// </summary>
    [DataField]
    public int Size = 100;

    /// <summary>
    /// If disabled, nothing can be buckled on this object, and it will unbuckle anything that's already buckled
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// The sound to be played when a mob is buckled
    /// </summary>
    [DataField]
    public SoundSpecifier BuckleSound  = new SoundPathSpecifier("/Audio/Effects/buckle.ogg");

    /// <summary>
    /// The sound to be played when a mob is unbuckled
    /// </summary>
    [DataField]
    public SoundSpecifier UnbuckleSound = new SoundPathSpecifier("/Audio/Effects/unbuckle.ogg");

    /// <summary>
    /// ID of the alert to show when buckled
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> BuckledAlertType = "Buckled";

    /// <summary>
    /// How long it takes to buckle someone else into a chair
    /// </summary>
    [DataField]
    public float BuckleDoafterTime = 2f;

    /// <summary>
    /// Whether InteractHand will buckle the user to the strap.
    /// </summary>
    [DataField]
    public bool BuckleOnInteractHand = true;
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
