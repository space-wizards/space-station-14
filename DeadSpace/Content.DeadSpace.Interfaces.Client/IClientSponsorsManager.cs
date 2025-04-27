using System.Diagnostics.CodeAnalysis;
using Content.DeadSpace.Interfaces.Shared;

namespace Content.DeadSpace.Interfaces.Client;

public interface IClientSponsorsManager : ISharedSponsorsManager
{
    bool TryGetInfo([NotNullWhen(true)] out ISponsorInfo? sponsor);
    bool TryGetSponsorList([NotNullWhen(false)] out ISponsorInfo[]? sponsors);
}
