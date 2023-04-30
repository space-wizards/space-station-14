using Content.Shared.Construction.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Xenoarchaeology.Equipment.Components;

/// <summary>
/// This is used for a machine that biases
/// an artifact placed on it to move up/down
/// </summary>
[RegisterComponent]
public sealed class TraversalDistorterComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float BiasChance;

    [DataField("baseBiasChance")]
    public float BaseBiasChance = 0.7f;

    [DataField("machinePartBiasChance", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
    public string MachinePartBiasChance = "Manipulator";

    [DataField("partRatingBiasChance")]
    public float PartRatingBiasChance = 1.1f;

    [ViewVariables(VVAccess.ReadWrite)]
    public BiasDirection BiasDirection = BiasDirection.In;

    public TimeSpan NextActivation = default!;
    public TimeSpan ActivationDelay = TimeSpan.FromSeconds(1);
}

public enum BiasDirection : byte
{
    In, //down the tree, towards depth 0
    Out //up the tree, away from depth 0
}
