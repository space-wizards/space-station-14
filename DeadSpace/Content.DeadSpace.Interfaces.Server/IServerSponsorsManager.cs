using System.Diagnostics.CodeAnalysis;
using Content.DeadSpace.Interfaces.Shared;
using Robust.Shared.Network;

namespace Content.DeadSpace.Interfaces.Server;

public interface IServerSponsorsManager : ISharedSponsorsManager
{
    bool TryGetInfo(NetUserId userId, [NotNullWhen(true)] out ISponsorInfo? sponsor);
    bool TryCalcAntagPriority(NetUserId userId);
}
