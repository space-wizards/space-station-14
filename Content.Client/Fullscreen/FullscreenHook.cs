using Content.Shared.Input;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Client.Fullscreen;
public sealed class FullscreenHook
{
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        _inputManager.SetInputCommand(ContentKeyFunctions.ToggleFullscreen, InputCmdHandler.FromDelegate(ToggleFullscreen));
        _sawmill = _logManager.GetSawmill("fullscreen");
    }

    private void ToggleFullscreen(ICommonSession? session)
    {
        var currentWindowMode = _cfg.GetCVar(CVars.DisplayWindowMode);

        switch (currentWindowMode)
        {
            case (int) WindowMode.Windowed:
                _cfg.SetCVar(CVars.DisplayWindowMode, (int) WindowMode.Fullscreen);
                _sawmill.Info("Switched to Fullscreen mode");
                break;

            case (int) WindowMode.Fullscreen:
                _cfg.SetCVar(CVars.DisplayWindowMode, (int) WindowMode.Windowed);
                _sawmill.Info("Switched to Windowed mode");
                break;

            default:
                throw new InvalidOperationException($"Unexpected WindowMode value: {currentWindowMode}");
        }
    }
}
