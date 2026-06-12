using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Offbrand.Medical;

/// <summary>
/// Organs with this component can be manually examined.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PalpatableOrganComponent : Component
{
    /// <summary>
    ///     Time it takes to palpate this organ.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     The thresholds for which perfusions will report which pulse qualities.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<float, PulseQuality> PulseQualities;

    /// <summary>
    ///     The thresholds for which strains will report which pulse speeds.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<float, LocId> PulseSpeeds;
}

[Serializable, NetSerializable]
public enum PulseQuality : byte
{
    Normal,
    SlightlyWeak,
    Weak,
    Weakest,
}
