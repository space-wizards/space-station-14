using System.Diagnostics.CodeAnalysis;
using Content.DeadSpace.Interfaces.Shared;
using Robust.Shared.Network;

namespace Content.DeadSpace.Interfaces.Server;

public interface IServerSponsorsManager : ISharedSponsorsManager
{
    public bool TryGetInfo(NetUserId userId, [NotNullWhen(true)] out ISponsorInfo? sponsor);
    public bool TryCalcAntagPriority(NetUserId userId);
}
