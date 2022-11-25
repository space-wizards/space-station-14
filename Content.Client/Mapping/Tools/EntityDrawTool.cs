using Content.Client.Mapping.Tools.Widgets;
using Content.Shared.Mapping;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Client.Mapping.Tools;

public sealed class EntityDrawTool : DrawingLikeTool
{
    [Dependency] private readonly IUserInterfaceManager _userInterface = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IEntitySystemManager _entSys = default!;
    [Dependency] private readonly IEntityNetworkManager _entNet = default!;

    public override SpriteSpecifier ToolActivityIcon =>
        new SpriteSpecifier.Texture(new ResourcePath("/Textures/Mapping/gray_checker.png"));

    public override Type ToolConfigurationControl => typeof(DrawToolWidget);

    private DrawToolWidget GetConfig()
    {
        return _userInterface.ActiveScreen!.GetWidget<DrawToolWidget>()!;
    }

    protected override bool ValidateNewInitialPoint(EntityCoordinates @new)
    {
        if (@new.TryDistance(_entMan, InitialClickPoint!.Value, out var dist) && dist > 1.0f)
        {
            return true;
        }

        return false;
    }

    public override bool ValidateDrawPoint(EntityCoordinates point)
    {
        return true;
    }

    public override bool ValidateDrawLine(EntityCoordinates start, EntityCoordinates end)
    {
        return true;
    }

    public override bool ValidateDrawRect(EntityCoordinates start, EntityCoordinates end)
    {
        return true;
    }

    public override void PreviewDrawPoint(EntityCoordinates point, in OverlayDrawArgs args)
    {
    }

    public override void PreviewDrawLine(EntityCoordinates start, EntityCoordinates end, in OverlayDrawArgs args)
    {
    }

    public override void PreviewDrawRect(EntityCoordinates start, EntityCoordinates end, in OverlayDrawArgs args)
    {
    }

    public override bool DoDrawPoint(EntityCoordinates point)
    {
        var config = GetConfig();
        _entNet.SendSystemNetworkMessage(new MappingDrawToolDrawEntityPointEvent(config.Prototype, EntityCoordinates.Invalid, config.Rotation, point));
        return true;
    }

    public override bool DoDrawLine(EntityCoordinates start, EntityCoordinates end)
    {

        return true;
    }

    public override bool DoDrawRect(EntityCoordinates start, EntityCoordinates end)
    {

        return true;
    }

    public EntityDrawTool()
    {
        IoCManager.InjectDependencies(this);
    }
}
