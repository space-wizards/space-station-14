using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.GameStates;

namespace Content.Shared.Abilities.Firestarter;

/// <summary>
/// Lets its owner entity ignite flammables around it and also heal some damage.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedFirestarterSystem))]
public sealed partial class FirestarterComponent : Component
{
    /// <summary>
    /// Radius of objects that will be ignited if flammable.
    /// </summary>
    [DataField("ignitionRadius")]
    public float IgnitionRadius = 4f;

    /// <summary>
    /// The action entity.
    /// </summary>
    [DataField("fireStarterAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? FireStarterAction = "ActionFireStarter";

    [DataField("fireStarterActionEntity")] public EntityUid? FireStarterActionEntity;


    /// <summary>
    /// Radius of objects that will be ignited if flammable.
    /// </summary>
    [DataField("igniteSound")]
    public SoundSpecifier IgniteSound = new SoundPathSpecifier("/Audio/Magic/rumble.ogg");
}
