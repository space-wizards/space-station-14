using Content.Shared.Materials;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Construction.Components;

/// <summary>
/// This is used for a machine that creates flatpacks at the cost of materials
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedFlatpackSystem))]
[AutoGenerateComponentState]
public sealed partial class FlatpackCreatorComponent : Component
{
    /// <summary>
    /// Whether or not packing is occuring
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool Packing;

    /// <summary>
    /// The time at which packing ends
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public TimeSpan PackEndTime;

    /// <summary>
    /// How long packing lasts.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan PackDuration = TimeSpan.FromSeconds(3);

    /// <summary>
    /// The prototype used when spawning a flatpack.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId BaseFlatpackPrototype = "BaseFlatpack";

    /// <summary>
    /// A default cost applied to all flatpacks outside of the cost of constructing the machine.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<MaterialPrototype>, int> BaseMaterialCost = new();

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string SlotId = "board_slot";
}

[Serializable, NetSerializable]
public enum FlatpackCreatorUIKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum FlatpackCreatorVisuals : byte
{
    Packing
}

[Serializable, NetSerializable]
public sealed class FlatpackCreatorStartPackBuiMessage : BoundUserInterfaceMessage
{

}
