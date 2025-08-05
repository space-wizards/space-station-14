using Content.Client.UserInterface.Systems.Chat;
using Content.Shared.Chat;
using Content.Shared.Inventory;
using Content.Shared.Radio.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Starlight.CCVar;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.Player;
using YamlDotNet.Core.Tokens;

namespace Content.Client._Starlight.Radio.Systems;

/// <summary>
/// This system handles playing radio chime sounds on the client side when radio messages are received.
/// </summary>
public sealed class RadioChimeSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public bool IsMuted = false;
    private bool _ttsEnabled = false;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, StarlightCCVars.TTSClientEnabled, x => _ttsEnabled = x, true);
        Subs.CVar(_cfg, StarlightCCVars.RadioChimeMuted, x => IsMuted = x, true);
    }

    public void PlayChime(SoundSpecifier? chimeSound)
    {
        if (chimeSound is not SoundSpecifier chime
            || IsMuted
            || _ttsEnabled)
            return;

        _audio.PlayGlobal(_audio.ResolveSound(chime), Filter.Local(), true, AudioParams.Default.WithVolume(-10f));
    }
}
