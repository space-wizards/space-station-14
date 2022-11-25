using Content.Client.Hands;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Screens;
using Content.Client.Viewport;
using Content.Shared.CCVar;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;

namespace Content.Client.Mapping.Overlays;

public sealed class MappingActivityOverlay : Overlay
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterface = default!;

    private readonly IRenderTexture _renderBackbuffer;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public MappingActivityOverlay()
    {
        IoCManager.InjectDependencies(this);

        _renderBackbuffer = _clyde.CreateRenderTarget(
            (64, 64),
            new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb, true),
            new TextureSampleParameters
            {
                Filter = true
            }, nameof(MappingActivityOverlay));
    }

    protected override void DisposeBehavior()
    {
        base.DisposeBehavior();

        _renderBackbuffer.Dispose();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var spriteSys = _entMan.EntitySysManager.GetEntitySystem<SpriteSystem>();
        var screen = args.ScreenHandle;
        var offset = _cfg.GetCVar(CCVars.HudHeldItemOffset);
        var mousePos = _inputManager.MouseScreenPosition.Position;
        var worldPos = _eye.ScreenToMap(mousePos).Position;

        if (_userInterface.ActiveScreen is not MappingGameScreen mapScreen)
            return;

        if (mapScreen.ActiveTool is not { } tool)
            return;

        tool.Draw(args);

        var f0 = spriteSys.Frame0(tool.ToolActivityIcon);
        screen.DrawTexture(f0, worldPos - f0.Size / 2 + offset, Color.White.WithAlpha(0.75f));

    }
}
