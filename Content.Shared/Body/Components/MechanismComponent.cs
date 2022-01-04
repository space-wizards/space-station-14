using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Body.Components;

[RegisterComponent]
[Friend(typeof(MechanismSystem))]
[ComponentProtoName("Mechanism")]
public class MechanismComponent : Component
{
    public SharedBodyComponent? Body => Part?.Body;

    public SharedBodyPartComponent? Part;

    // TODO BODY OnSizeChanged
    /// <summary>
    ///     Determines whether this
    ///     <see cref="MechanismComponent" /> can fit into a <see cref="SharedBodyPartComponent" />.
    /// </summary>
    [DataField("size")]
    public int Size { get; set; } = 1;

    /// <summary>
    ///     What kind of <see cref="SharedBodyPartComponent" /> this
    ///     <see cref="MechanismComponent" /> can be easily installed into.
    /// </summary>
    [DataField("compatibility")]
    public BodyPartCompatibility Compatibility { get; set; } = BodyPartCompatibility.Universal;
}
