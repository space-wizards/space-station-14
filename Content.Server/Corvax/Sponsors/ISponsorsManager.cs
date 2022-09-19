using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Network;

namespace Content.Server.Corvax.Sponsors;

public interface ISponsorsManager
{
    void Initialize();

    /**
     * Gets the cached color of the players OOC if he is a sponsor
     */
    bool TryGetCustomOOCColor(NetUserId userId, [MaybeNullWhen(false)] out string color);
}
