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

    [AutoNetworkedField] public EntityUid? Parent;

    [AutoNetworkedField] public string? SlotId = null;

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

    [AutoNetworkedField(CloneData = true)] public Dictionary<string, BodyPartSlot> Children = new();

    [AutoNetworkedField(CloneData = true)] public Dictionary<string, OrganSlot> Organs = new();

}
[NetSerializable, Serializable]
public readonly record struct BodyPartSlot(string Id, BodyPartType Type, [field: NonSerialized] ContainerSlot Container)
{
    public EntityUid? Entity => Container.ContainedEntity;
};

[NetSerializable, Serializable]
public readonly record struct OrganSlot(string Id, [field: NonSerialized] ContainerSlot Container)
{
    public EntityUid? Organ => Container.ContainedEntity;
};
