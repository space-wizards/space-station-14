using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Components;

[RegisterComponent, NetworkedComponent]
public sealed class BodyPartComponent : Component
{
    [ViewVariables] public EntityUid? Body;

    [ViewVariables]
    [DataField("parent")]
    public BodyPartSlot? ParentSlot;

    [ViewVariables]
    [DataField("children")]
    public Dictionary<string, BodyPartSlot> Children = new();

    [ViewVariables]
    [DataField("organs")]
    public Dictionary<string, OrganSlot> Organs = new();

    [ViewVariables]
    [DataField("partType")]
    public BodyPartType PartType = BodyPartType.Other;

    // TODO BODY Replace with a simulation of organs
    /// <summary>
    ///     Whether or not the owning <see cref="Body"/> will die if all
    ///     <see cref="BodyComponent"/>s of this type are removed from it.
    /// </summary>
    [ViewVariables]
    [DataField("vital")]
    public bool IsVital;

    [ViewVariables]
    [DataField("symmetry")]
    public BodyPartSymmetry Symmetry = BodyPartSymmetry.None;
}
