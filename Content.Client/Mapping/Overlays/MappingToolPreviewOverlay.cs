using Content.Client.UserInterface.Screens;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;

namespace Content.Client.Mapping.Overlays;

public sealed class MappingToolPreviewOverlay : Overlay
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterface = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public MappingToolPreviewOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_userInterface.ActiveScreen is not MappingGameScreen mapScreen)
            return;

        if (mapScreen.ActiveTool is not { } tool)
            return;

        tool.Draw(args);
    }
}
