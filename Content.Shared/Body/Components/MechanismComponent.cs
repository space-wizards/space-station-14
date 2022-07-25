using Content.Shared.Body.Part;
using Content.Shared.Body.Systems.Part;

namespace Content.Shared.Body.Components;

[RegisterComponent]
[Access(typeof(SharedBodyPartSystem))]
public sealed class MechanismComponent : Component
{
    public SharedBodyComponent? Body => Part?.Body;

    public SharedBodyPartComponent? Part;

    // TODO BODY OnSizeChanged
    /// <summary>
    ///     Determines whether this
    ///     <see cref="MechanismComponent" /> can fit into a <see cref="SharedBodyPartComponent" />.
    /// </summary>
    [DataField("size")]
    public int Size = 1;

    /// <summary>
    ///     What kind of <see cref="SharedBodyPartComponent" /> this
    ///     <see cref="MechanismComponent" /> can be easily installed into.
    /// </summary>
    [DataField("compatibility")]
    public BodyPartCompatibility Compatibility = BodyPartCompatibility.Universal;
}
