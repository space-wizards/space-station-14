using Robust.Shared.Serialization;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Cargo;

/// <summary>
/// A data structure for storing currently available bounties.
/// </summary>
[DataDefinition, NetSerializable, Serializable]
public readonly partial record struct CargoBountyData(int Id, string Bounty, TimeSpan EndTime)
{
    /// <summary>
    /// A numeric id used to identify the bounty
    /// </summary>
    [DataField("id"), ViewVariables(VVAccess.ReadWrite)]
    public int Id { get; init; } = Id;

    /// <summary>
    /// The prototype containing information about the bounty.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("bounty", customTypeSerializer: typeof(PrototypeIdSerializer<CargoBountyPrototype>), required:true)]
    public string Bounty { get; init; } = Bounty;

    /// <summary>
    /// The time at which the bounty is closed and no longer is available.
    /// </summary>
    [DataField("endTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan EndTime { get; init; } = EndTime;

    public CargoBountyData() : this(default, string.Empty, default)
    {
    }
}
