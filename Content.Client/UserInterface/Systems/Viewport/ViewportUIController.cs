using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Systems.Viewport;

public sealed class ViewportUIController : UIController
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IConsoleHost _conHost = default!;

    public const int ViewportHeight = 15;

    public MainViewport? Viewport => (UIManager.ActiveScreen?.GetWidget<MainViewport>() ?? UIManager.ActiveScreen?.GetWidget<SplitViewportWidget>());

    public override void Initialize()
    {
        _configurationManager.OnValueChanged(CCVars.ViewportMinimumWidth, _ => UpdateViewportRatio());
        _configurationManager.OnValueChanged(CCVars.ViewportMaximumWidth, _ => UpdateViewportRatio());
        _configurationManager.OnValueChanged(CCVars.ViewportWidth, _ => UpdateViewportRatio());

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
        gameplayStateLoad.OnScreenUnload += OnUnload;
    }

    private void OnScreenLoad()
    {
        ReloadViewport();
        _conHost.RegisterCommand($"split_viewport",
            Loc.GetString("cmd-split-viewport-help"),
            Loc.GetString("cmd-split-viewport-description"),
            SplitViewport);
    }

    private void OnUnload()
    {
        _conHost.UnregisterCommand($"split_viewport");
    }

    private void SplitViewport(IConsoleShell shell, string argstr, string[] args)
    {
        if (UIManager.ActiveScreen is not {} screen)
            return;

        MainViewport newViewport;
        if (screen.TryGetWidget<MainViewport>(out var old))
        {
            var oldParent = old.Parent;
            screen.RemoveWidget<MainViewport>();
            newViewport = new SplitViewportWidget();
            oldParent!.AddChild(newViewport);
            if (oldParent != screen)
                screen.AddWidgetDirect(newViewport);
        }
        else if (screen.TryGetWidget<SplitViewportWidget>(out var oldSplit))
        {
            var oldParent = oldSplit.Parent;
            screen.RemoveWidget<SplitViewportWidget>();
            newViewport = new MainViewport();
            oldParent!.AddChild(newViewport);
            if (oldParent != screen)
                screen.AddWidgetDirect(newViewport);
        }

        ReloadViewport();
    }

    private void UpdateViewportRatio()
    {
        if (Viewport == null)
        {
            return;
        }

        var min = _configurationManager.GetCVar(CCVars.ViewportMinimumWidth);
        var max = _configurationManager.GetCVar(CCVars.ViewportMaximumWidth);
        var width = _configurationManager.GetCVar(CCVars.ViewportWidth);

        if (width < min || width > max)
        {
            width = CCVars.ViewportWidth.DefaultValue;
        }

        Viewport.ViewportSize = (EyeManager.PixelsPerMeter * width, EyeManager.PixelsPerMeter * ViewportHeight);
    }

    public void ReloadViewport()
    {
        if (Viewport == null)
        {
            return;
        }

        UpdateViewportRatio();
        _eyeManager.MainViewport = Viewport.Viewport;
        Viewport.UpdateCfg();
    }

    public override void FrameUpdate(FrameEventArgs e)
    {
        if (Viewport == null)
        {
            return;
        }

        base.FrameUpdate(e);

        Viewport.Eye = _eyeManager.CurrentEye;

        // verify that the current eye is not "null". Fuck IEyeManager.

        var ent = _playerMan.LocalPlayer?.ControlledEntity;
        if (_eyeManager.CurrentEye.Position != default || ent == null)
            return;

        _entMan.TryGetComponent(ent, out EyeComponent? eye);

        if (eye?.Eye == _eyeManager.CurrentEye
            && _entMan.GetComponent<TransformComponent>(ent.Value).WorldPosition == default)
            return; // nothing to worry about, the player is just in null space... actually that is probably a problem?

        // Currently, this shouldn't happen. This likely happened because the main eye was set to null. When this
        // does happen it can create hard to troubleshoot bugs, so lets print some helpful warnings:
        Logger.Warning($"Main viewport's eye is in nullspace (main eye is null?). Attached entity: {_entMan.ToPrettyString(ent.Value)}. Entity has eye comp: {eye != null}");
    }
}
