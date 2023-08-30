using Content.Shared.Physics;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Singularity.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ContainmentFieldGeneratorComponent : Component
{
        private int _powerBuffer;

    /// <summary>
    /// Store power with a cap. Decrease over time if not being powered from source.
    /// </summary>
    [DataField("powerBuffer")]
    public int PowerBuffer
    {
        get => _powerBuffer;
        set => _powerBuffer = Math.Clamp(value, 0, 25); //have this decrease over time if not hit by a bolt
    }

    /// <summary>
    /// The minimum the field generator needs to start generating a connection
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("powerMinimum")]
    public int PowerMinimum = 6;

    /// <summary>
    /// How much power should this field generator receive from a collision
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("power")]
    public int PowerReceived = 3;

    /// <summary>
    /// How much power should this field generator lose if not powered?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("powerLoss")]
    public int PowerLoss = 2;

    /// <summary>
    /// Used to check if it's received power recently.
    /// </summary>
    [DataField("accumulator")]
    public float Accumulator;

    /// <summary>
    /// How many seconds should the generators wait before losing power?
    /// </summary>
    [DataField("threshold")]
    public float Threshold = 10f;

    /// <summary>
    /// How many tiles should this field check before giving up?
    /// </summary>
    [DataField("maxLength")]
    public float MaxLength = 8F;

    /// <summary>
    /// What collision should power this generator?
    /// It really shouldn't be anything but an emitter bolt but it's here for fun.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("idTag", customTypeSerializer: typeof(PrototypeIdSerializer<TagPrototype>))]
    public string IDTag = "EmitterBolt";

    /// <summary>
    /// Is the generator toggled on?
    /// </summary>
    [ViewVariables]
    public bool Enabled;

    /// <summary>
    /// Is this generator connected to fields?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsConnected;

    /// <summary>
    /// The masks the raycast should not go through
    /// </summary>
    [DataField("collisionMask")]
    public int CollisionMask = (int) (CollisionGroup.MobMask | CollisionGroup.Impassable | CollisionGroup.MachineMask | CollisionGroup.Opaque);

    /// <summary>
    /// A collection of connections that the generator has based on direction.
    /// Stores a list of fields connected between generators in this direction.
    /// </summary>
    [ViewVariables]
    public Dictionary<Direction, (ContainmentFieldGeneratorComponent, List<EntityUid>)> Connections = new();

    /// <summary>
    /// What fields should this spawn?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("createdField", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string CreatedField = "ContainmentField";
}

[Serializable, NetSerializable]
public enum ContainmentFieldGeneratorVisuals : byte
{
    PowerLight,
    FieldLight,
    OnLight,
}

[Serializable, NetSerializable]
public enum PowerLevelVisuals : byte
{
    NoPower,
    LowPower,
    MediumPower,
    HighPower,
}

[Serializable, NetSerializable]
public enum FieldLevelVisuals : byte
{
    NoLevel,
    On,
    OneField,
    MultipleFields,
}
