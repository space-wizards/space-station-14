using Robust.Shared.Serialization;

namespace Content.Shared.RussStation.Botany;

/// <summary>
/// Stats for one group of seeds stored in the extractor, sent to the client for UI display.
/// Seeds with identical stats are grouped; Count reflects how many packets of that type are stored.
/// </summary>
[Serializable, NetSerializable]
public sealed class SeedExtractorStorageSeedData
{
    /// <summary>Localized plant display name, e.g. "orange".</summary>
    public string DisplayName = string.Empty;

    /// <summary>Entity prototype ID of the seed packet, used by the client to render the icon.</summary>
    public string PacketPrototype = string.Empty;

    /// <summary>
    /// Opaque key that uniquely identifies this stats profile.
    /// Sent back to the server in <see cref="SeedExtractorStorageTakeSeedMessage"/> to identify which group to take from.
    /// </summary>
    public string GroupKey = string.Empty;

    /// <summary>How many seed packets share this stats profile.</summary>
    public int Count;

    public float Potency;
    public int Yield;
    public float Endurance;
    public float Lifespan;
    public float Maturation;
    public float Production;
}

[Serializable, NetSerializable]
public sealed class SeedExtractorStorageUpdateState : BoundUserInterfaceState
{
    public List<SeedExtractorStorageSeedData> Seeds;

    public SeedExtractorStorageUpdateState(List<SeedExtractorStorageSeedData> seeds)
    {
        Seeds = seeds;
    }
}

/// <summary>
/// Sent by the client to take one seed packet from the group identified by <see cref="GroupKey"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed class SeedExtractorStorageTakeSeedMessage : BoundUserInterfaceMessage
{
    /// <summary>
    /// The group key from <see cref="SeedExtractorStorageSeedData.GroupKey"/> that identifies which stats profile to take from.
    /// </summary>
    public string GroupKey = string.Empty;

    public SeedExtractorStorageTakeSeedMessage(string groupKey)
    {
        GroupKey = groupKey;
    }
}

[NetSerializable, Serializable]
public enum SeedExtractorStorageUiKey
{
    Key,
}
