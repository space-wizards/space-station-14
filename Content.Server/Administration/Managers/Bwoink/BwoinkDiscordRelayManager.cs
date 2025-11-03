using Content.Server.Discord.DiscordLink;
using Content.Shared.Administration.Managers.Bwoink;
using Content.Shared.Administration.Managers.Bwoink.Features;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Managers.Bwoink;

/// <summary>
/// Handles all the logic around the <see cref="DiscordRelay"/> channel feature.
/// </summary>
public sealed class BwoinkDiscordRelayManager : IPostInjectInit
{
    [Dependency] private readonly ILogManager _logManager = null!;
    [Dependency] private readonly DiscordLink _discordLink = null!;
    [Dependency] private readonly ServerBwoinkManager _serverBwoinkManager = null!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = null!;

    // ReSharper disable once InconsistentNaming
    private ISawmill Log = null!;



    void IPostInjectInit.PostInject()
    {
        Log = _logManager.GetSawmill("bwoink.discord");
    }

    public void Initialize()
    {
        _serverBwoinkManager.MessageReceived += BwoinkReceived;
    }

    private void BwoinkReceived(ProtoId<BwoinkChannelPrototype> sender, (NetUserId person, BwoinkMessage message) args)
    {
        
    }
}
