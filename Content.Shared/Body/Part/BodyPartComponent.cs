using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Part;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBodySystem))]
public sealed class BodyPartComponent : Component
{
    [DataField("body")] public EntityUid? Body;

    [DataField("parent")] public BodyPartSlot? ParentSlot;

    [DataField("children")] public Dictionary<string, BodyPartSlot> Children = new();

    [DataField("organs")] public Dictionary<string, OrganSlot> Organs = new();

    [DataField("partType")] public BodyPartType PartType = BodyPartType.Other;

    // TODO BODY Replace with a simulation of organs
    /// <summary>
    ///     Whether or not the owning <see cref="Body"/> will die if all
    ///     <see cref="BodyComponent"/>s of this type are removed from it.
    /// </summary>
    [DataField("vital")] public bool IsVital;

    [DataField("symmetry")] public BodyPartSymmetry Symmetry = BodyPartSymmetry.None;

    [DataField("painModifier")] public FixedPoint2 PainModifier = 1.0f;

    [DataField("rawPain")] public FixedPoint2 RawPain = 0f;

    public FixedPoint2 Pain => PainModifier * RawPain;
}
