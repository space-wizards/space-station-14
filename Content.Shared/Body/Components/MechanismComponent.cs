using Content.Shared.Body.Prototypes;
using Content.Shared.Body.Systems.Part;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Body.Components;

[RegisterComponent]
[Access(typeof(SharedBodyPartSystem))]
public sealed class MechanismComponent : Component
{
    public SharedBodyComponent? Body => Part?.Body;

    public SharedBodyPartComponent? Part;

    /// <summary>
    ///     Determines whether this
    ///     <see cref="MechanismComponent" /> can fit into a <see cref="SharedBodyPartComponent" />.
    /// </summary>
    [DataField("size")]
    public int Size = 1;

    /// <summary>
    ///     What kind of <see cref="SharedBodyPartComponent" /> this
    ///     <see cref="MechanismComponent" /> can be easily installed into.
    ///     If no compatibility is set, the mechanism is considered universal.
    /// </summary>
    [DataField("compatibility", customTypeSerializer: typeof(PrototypeIdSerializer<BodyPartCompatibilityPrototype>))]
    public string? Compatibility = null;
}
