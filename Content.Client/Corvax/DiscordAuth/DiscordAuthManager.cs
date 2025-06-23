using System.IO;
using System.Threading;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Corvax.DiscordAuth;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client.Corvax.DiscordAuth;

public sealed class DiscordAuthManager
{
    [Dependency] private readonly IClientNetManager _netManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public string AuthUrl { get; private set; } = string.Empty;
    public Texture? Qrcode { get; private set; }
    public bool IsVerified { get; private set; } = true;
    public bool IsOpt { get; private set; }
    public bool IsEnabled { get; private set; }

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgDiscordAuthCheck>();
        _netManager.RegisterNetMessage<MsgDiscordAuthByPass>();
        _netManager.RegisterNetMessage<MsgDiscordAuthRequired>(OnDiscordAuthRequired);

        _cfg.OnValueChanged(CCCVars.DiscordAuthIsOptional, v => IsOpt = v, true);
        _cfg.OnValueChanged(CCCVars.DiscordAuthEnabled, v => IsEnabled = v, true);
    }

    private void OnDiscordAuthRequired(MsgDiscordAuthRequired message)
    {
        if (_stateManager.CurrentState is not DiscordAuthState)
        {
            AuthUrl = message.AuthUrl;
            if (message.QrCode.Length > 0)
            {
                using var ms = new MemoryStream(message.QrCode);
                Qrcode = Texture.LoadFromPNGStream(ms);
            }

            _stateManager.RequestStateChange<DiscordAuthState>();
        }
    }

    public void ByPass()
    {
        IsVerified = false;
        _netManager.ClientSendMessage(new MsgDiscordAuthByPass());
    }
}