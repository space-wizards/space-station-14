using Content.Shared.Physics;
using Content.Shared.Singularity.Components;

namespace Content.Server.Singularity.Components;
[RegisterComponent]
[ComponentReference(typeof(SharedContainmentFieldGeneratorComponent))]
public sealed class ContainmentFieldGeneratorComponent : SharedContainmentFieldGeneratorComponent
{
    [ViewVariables]
    private int _powerBuffer;

    /// <summary>
    /// Store power with a cap. Decrease over time if not being powered from source.
    /// </summary>
    [ViewVariables]
    public int PowerBuffer
    {
        get => _powerBuffer;
        set => _powerBuffer = Math.Clamp(value, 0, 25); //have this decrease over time if not hit by a bolt
    }

    /// <summary>
    /// How far should this field check before giving up?
    /// </summary>
    [ViewVariables]
    [DataField("maxLength")]
    public float MaxLength = 8F;

    /// <summary>
    /// What collision should power this generator?
    /// It really shouldn't be anything but an emitter bolt but it's here for fun.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("idTag")]
    public string IDTag = "EmitterBolt";

    /// <summary>
    /// How much power should this field generator receive from a collision
    /// Also acts as the minimum the field needs to start generating a connection
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("power")]
    public int Power = 6;

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
    [ViewVariables]
    [DataField("collisionMask")]
    public int CollisionMask = (int) (CollisionGroup.MobMask | CollisionGroup.Impassable | CollisionGroup.MachineMask);

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
    [DataField("createdField")]
    public string CreatedField = "ContainmentField";
}
