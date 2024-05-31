using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Salvage.Magnet;

/// <summary>
/// Added to the station to hold salvage magnet data.
/// </summary>
[RegisterComponent]
public sealed partial class SalvageMagnetDataComponent : Component
{
    // May be multiple due to splitting.

    /// <summary>
    /// Entities currently magnetised.
    /// </summary>
    [DataField]
    public List<EntityUid>? ActiveEntities;

    /// <summary>
    /// If the magnet is currently active when does it end.
    /// </summary>
    [DataField(customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan? EndTime;

    [DataField(customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextOffer;

    /// <summary>
    /// How long salvage will be active for before despawning.
    /// </summary>
    [DataField]
    public TimeSpan ActiveTime = TimeSpan.FromMinutes(6);

    /// <summary>
    /// Cooldown between offerings after one ends.
    /// </summary>
    [DataField]
    public TimeSpan OfferCooldown = TimeSpan.FromMinutes(3);

    /// <summary>
    /// Seeds currently offered
    /// </summary>
    [DataField]
    public List<int> Offered = new();

    [DataField]
    public int OfferCount = 6;

    [DataField]
    public int ActiveSeed;

    /// <summary>
    /// Final countdown announcement.
    /// </summary>
    [DataField]
    public bool Announced;
}
