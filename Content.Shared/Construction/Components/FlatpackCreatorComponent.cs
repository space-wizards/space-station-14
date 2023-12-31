using Content.Shared.Materials;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Construction.Components;

/// <summary>
/// This is used for a machine that creates flatpacks at the cost of materials
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedFlatpackSystem))]
public sealed partial class FlatpackCreatorComponent : Component
{
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
}
