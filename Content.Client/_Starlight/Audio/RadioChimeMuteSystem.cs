using Content.Shared.Radio;
using Content.Shared.Starlight.CCVar;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Client.Audio;

/// <summary>
/// This system handles muting radio chimes based on the user's settings.
/// </summary>
public sealed class RadioChimeMuteSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    private bool _radioChimeMuted = false;

    public override void Initialize()
    {
        base.Initialize();
        
        // Subscribe to the RadioChimeMuted CVar
        Subs.CVar(_cfg, StarlightCCVars.RadioChimeMuted, ToggleRadioChimeMuted, true);
    }

    private void ToggleRadioChimeMuted(bool muted)
    {
        _radioChimeMuted = muted;
        
        // Send the preference to the server
        RaiseNetworkEvent(new RadioChimeMuteEvent(muted));
    }
}
