using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Part;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedBodySystem))]
public sealed partial class BodyPartComponent : Component
{
    [DataField("body"), AutoNetworkedField]
    public EntityUid? Body;

    [AutoNetworkedField, ViewVariables] public EntityUid? Parent;

    [AutoNetworkedField, ViewVariables] public string? AttachedToSlot = null;

    [DataField("partType"), AutoNetworkedField]
    public BodyPartType PartType = BodyPartType.Other;

    // TODO BODY Replace with a simulation of organs
    /// <summary>
    ///     Whether or not the owning <see cref="Body"/> will die if all
    ///     <see cref="BodyComponent"/>s of this type are removed from it.
    /// </summary>
    [DataField("vital"), AutoNetworkedField]
    public bool IsVital;

    [DataField("symmetry"), AutoNetworkedField]
    public BodyPartSymmetry Symmetry = BodyPartSymmetry.None;

    [AutoNetworkedField(CloneData = true), ViewVariables]
    public Dictionary<string, BodyPartSlot> Children = new();


    [AutoNetworkedField(CloneData = true), ViewVariables]
    public Dictionary<string, OrganSlot> Organs = new();


    /// <summary>
    /// These are only for VV/Debug do not use these for gameplay/systems
    /// </summary>
    [ViewVariables]
    private List<ContainerSlot> BodyPartSlotsVV
    {
        get
        {
            List<ContainerSlot> temp = new();
            foreach (var (_,slotData) in Children)
            {
                temp.Add(slotData.Container);
            }
            return temp;
        }
    }
    [ViewVariables]
    private List<ContainerSlot> OrganSlotsVV
    {
        get
        {
            List<ContainerSlot> temp = new();
            foreach (var (_,slotData) in Organs)
            {
                temp.Add(slotData.Container);
            }
            return temp;
        }
    }
}
[NetSerializable, Serializable]
public readonly record struct BodyPartSlot([field: ViewVariables] string Id,[field: ViewVariables] BodyPartType Type, [field: NonSerialized] ContainerSlot Container)
{
    [ViewVariables]
    public EntityUid? Entity => Container.ContainedEntity;
};

[NetSerializable, Serializable]
public readonly record struct OrganSlot([field: ViewVariables] string Name, [field: NonSerialized] ContainerSlot Container)
{
    [ViewVariables]
    public EntityUid? Organ => Container.ContainedEntity;
};
