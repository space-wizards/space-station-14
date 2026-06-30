using System.Numerics;
using Content.Client.Eui;
using Content.Shared.Afk;
using Content.Shared.CCVar;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Client.Afk;

[UsedImplicitly]
public sealed partial class AfkConfirmEui : BaseEui
{
    private const float MaxWindowOffset = 64f;

    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IRobustRandom _random = default!;
    private AudioSystem _audio;
    private SoundSpecifier _confirmSound;
    private EntityUid? _confirmSoundStream;

    private readonly AfkConfirmWindow _window = new();

    public AfkConfirmEui()
    {
        _audio = _entManager.System<AudioSystem>();
        _confirmSound = new SoundPathSpecifier(_cfg.GetCVar(CCVars.AfkConfirmSound));
        _cfg.OnValueChanged(CCVars.AfkConfirmSound, OnConfirmSoundChanged);

        _window.OnConfirm += () =>
        {
            SendMessage(new AfkConfirmMessage());
            _window.Close();
        };

        _window.OnClose += () => SendMessage(new CloseEuiMessage());
    }

    public override void Opened()
    {
        _clyde.RequestWindowAttention();
        _confirmSoundStream = _audio.PlayGlobal(_confirmSound, Filter.Local(), false)?.Entity;

        var screenSize = _clyde.ScreenSize;
        var screenSizeVector = new Vector2(screenSize.X, screenSize.Y);
        var offset = new Vector2(
            RandomOffset(),
            RandomOffset());
        var relativePosition = new Vector2(0.5f) + offset / screenSizeVector;

        _window.OpenCenteredAt(relativePosition);
    }

    private float RandomOffset()
    {
        return _random.NextFloat() * MaxWindowOffset * 2 - MaxWindowOffset;
    }

    private void OnConfirmSoundChanged(string path)
    {
        _confirmSound = new SoundPathSpecifier(path);
    }

    public override void Closed()
    {
        _cfg.UnsubValueChanged(CCVars.AfkConfirmSound, OnConfirmSoundChanged);
        _confirmSoundStream = _audio.Stop(_confirmSoundStream);
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not AfkConfirmEuiState afkState)
            return;

        _window.SetTimeRemaining(afkState.TimeRemaining);
    }
}
