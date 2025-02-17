using System.Diagnostics.CodeAnalysis;
using Content.DeadSpace.Interfaces.Shared;

namespace Content.DeadSpace.Interfaces.Client;

public interface IClientSponsorsManager : ISharedSponsorsManager
{
    public bool TryGetInfo([NotNullWhen(true)] out ISponsorInfo? sponsor);
    public bool TryGetSponsorList([NotNullWhen(false)] out ISponsorInfo[]? sponsors);
}
