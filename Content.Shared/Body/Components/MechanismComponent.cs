using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Body.Systems.Part;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Body.Components;

[RegisterComponent]
[Friend(typeof(MechanismSystem), typeof(SharedBodyPartSystem))]
public class MechanismComponent : Component
{
    public SharedBodyComponent? Body => Part?.Body;

    public SharedBodyPartComponent? Part;

    // TODO BODY Replace with a simulation of organs
    /// <summary>
    ///     Whether or not the owning <see cref="Body"/> will die if this mechanism is removed.
    /// </summary>
    [ViewVariables]
    [DataField("vital")]
    public bool IsVital;

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
