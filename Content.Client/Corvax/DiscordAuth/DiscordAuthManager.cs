using System.Threading;
using Content.Shared.Corvax.DiscordAuth;
using Robust.Client.State;
using Robust.Shared.Network;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client.Corvax.DiscordAuth;

public sealed class DiscordAuthManager
{
    [Dependency] private readonly IClientNetManager _netManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;

    public string AuthUrl { get; private set; } = string.Empty;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgDiscordAuthCheck>();
        _netManager.RegisterNetMessage<MsgDiscordAuthRequired>(OnDiscordAuthRequired);
    }

    private void OnDiscordAuthRequired(MsgDiscordAuthRequired message)
    {
        if (_stateManager.CurrentState is not DiscordAuthState)
        {
            AuthUrl = message.AuthUrl;
            _stateManager.RequestStateChange<DiscordAuthState>();
        }
    }
}
