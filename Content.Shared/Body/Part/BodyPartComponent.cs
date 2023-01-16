using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Part;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedBodySystem))]
public sealed class BodyPartComponent : Component
{
    [DataField("body")]
    [AutoNetworkedField]
    public EntityUid? Body;

    [DataField("parent")]
    [AutoNetworkedField]
    public BodyPartSlot? ParentSlot;

    [DataField("children")]
    [AutoNetworkedField(true)]
    public Dictionary<string, BodyPartSlot> Children = new();

    [DataField("organs")]
    [AutoNetworkedField(true)]
    public Dictionary<string, OrganSlot> Organs = new();

    [DataField("partType")]
    [AutoNetworkedField]
    public BodyPartType PartType = BodyPartType.Other;

    // TODO BODY Replace with a simulation of organs
    /// <summary>
    ///     Whether or not the owning <see cref="Body"/> will die if all
    ///     <see cref="BodyComponent"/>s of this type are removed from it.
    /// </summary>
    [DataField("vital")]
    [AutoNetworkedField]
    public bool IsVital;

    [DataField("symmetry")]
    [AutoNetworkedField]
    public BodyPartSymmetry Symmetry = BodyPartSymmetry.None;
}
