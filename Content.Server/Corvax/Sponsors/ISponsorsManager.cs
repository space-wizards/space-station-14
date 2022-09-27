using Robust.Shared.Network;

namespace Content.Server.Corvax.Sponsors;

public interface ISponsorsManager
{
    void Initialize();

    /**
     * Gets the cached on player join sponsor info
     */
    ISponsor? GetSponsorInfo(NetUserId userId);
}

public interface ISponsor
{
    int? Tier { get; }
    string? OOCColor { get; }
    bool HavePriorityJoin { get; }
}
