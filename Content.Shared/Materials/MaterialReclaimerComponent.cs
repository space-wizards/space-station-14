using Content.Shared.Chemistry.Components;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Materials;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedMaterialReclaimerSystem))]
public sealed class MaterialReclaimerComponent : Component
{
    /// <summary>
    /// Whether or not the machine has power. We put it here
    /// so we can network and predict it.
    /// </summary>
    [DataField("powered")]
    public bool Powered;

    /// <summary>
    /// How quickly it takes to consume X amount of materials per second.
    /// For example, with a rate of 50, an entity with 100 total material takes 2 seconds to process.
    /// </summary>
    [DataField("baseMaterialProcessRate")]
    public float BaseMaterialProcessRate = 50f;

    /// <summary>
    /// How quickly it takes to consume X amount of materials per second.
    /// For example, with a rate of 50, an entity with 100 total material takes 2 seconds to process.
    /// </summary>
    [DataField("materialProcessRate")]
    public float MaterialProcessRate;

    /// <summary>
    /// Machine part whose rating modifies <see cref="MaterialProcessRate"/>
    /// </summary>
    [DataField("machinePartProcessRate", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
    public string MachinePartProcessRate = "Manipulator";

    /// <summary>
    /// How much the machine part quality affects the <see cref="MaterialProcessRate"/>
    /// </summary>
    [DataField("partRatingProcessRateMultiplier")]
    public float PartRatingProcessRateMultiplier = 1.5f;

    /// <summary>
    /// The minimum amount fo time it can take to process an entity.
    /// this value supercedes the calculated one using <see cref="MaterialProcessRate"/>
    /// </summary>
    [DataField("minimumProcessDuration")]
    public TimeSpan MinimumProcessDuration = TimeSpan.FromSeconds(0.5f);

    /// <summary>
    /// The id of our output solution
    /// </summary>
    [DataField("solutionContainerId")]
    public string SolutionContainerId = "output";

    /// <summary>
    /// The prototype for the puddle
    /// </summary>
    [DataField("puddleId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string PuddleId = "PuddleSmear";

    /// <summary>
    /// The solution itself.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public Solution OutputSolution = default!;

    /// <summary>
    /// a whitelist for what entities can be inserted into this reclaimer
    /// </summary>
    [DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// a blacklist for what entities cannot be inserted into this reclaimer
    /// </summary>
    [DataField("blacklist")]
    public EntityWhitelist? Blacklist;
}

[Serializable, NetSerializable]
public sealed class MaterialReclaimerComponentState : ComponentState
{
    public bool Powered;

    public float MaterialProcessRate;

    public MaterialReclaimerComponentState(bool powered, float materialProcessRate)
    {
        Powered = powered;
        MaterialProcessRate = materialProcessRate;
    }
}

/// <summary>
/// Event raised when an entity is being reclaimed by a <see cref="MaterialReclaimerComponent"/>
/// </summary>
[ByRefEvent]
public readonly record struct GetMaterialReclaimedEvent;
