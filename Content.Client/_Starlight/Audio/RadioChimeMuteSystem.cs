using Content.Shared.Starlight.CCVar;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Configuration;

namespace Content.Client._Starlight.Audio;

/// <summary>
/// This system handles muting radio chimes based on the user's settings.
/// </summary>
public sealed class RadioChimeMuteSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    /// <summary>
    /// Whether radio chimes are currently muted.
    /// </summary>
    public bool IsMuted => _radioChimeMuted;

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
    }
}
