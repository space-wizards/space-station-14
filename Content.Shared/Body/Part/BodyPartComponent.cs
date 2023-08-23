using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Part;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBodySystem))]
public sealed partial class BodyPartComponent : Component
{
    [DataField("body")]
    public EntityUid? Body;

    // This inter-entity relationship makes be deeply uncomfortable because its probably going to re-encounter all of the
    // networking issues that containers and joints have.
    // TODO just use containers. Please.
    // Do not use set or get data from this in client-side code.
    public BodyPartSlot? ParentSlot;

    // Do not use set or get data from this in client-side code.
    [DataField("children")]
    public Dictionary<string, BodyPartSlot> Children = new();

    // See all the above ccomments.
    [DataField("organs")]
    public Dictionary<string, OrganSlot> Organs = new();

    [DataField("partType")]
    public BodyPartType PartType = BodyPartType.Other;

    // TODO BODY Replace with a simulation of organs
    /// <summary>
    ///     Whether or not the owning <see cref="Body"/> will die if all
    ///     <see cref="BodyComponent"/>s of this type are removed from it.
    /// </summary>
    [DataField("vital")]
    public bool IsVital;

    [DataField("symmetry")]
    public BodyPartSymmetry Symmetry = BodyPartSymmetry.None;
}
