using Content.Client.Mapping.Tools.Widgets;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Client.Mapping.Tools;

public sealed class DrawTool : DrawingLikeTool
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IEntitySystemManager _entSys = default!;
    [Dependency] private readonly IEntityNetworkManager _entNet = default!;

    public override SpriteSpecifier ToolActivityIcon =>
        new SpriteSpecifier.Texture(new ResourcePath("/Textures/Mapping/gray_checker.png"));

    public override Type ToolConfigurationControl => typeof(DrawToolWidget);

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
        if (!ValidateDrawPoint(point))
            return false;
        return true;
    }

    public override bool DoDrawLine(EntityCoordinates start, EntityCoordinates end)
    {
        if (!ValidateDrawLine(start, end))
            return false;
        return true;
    }

    public override bool DoDrawRect(EntityCoordinates start, EntityCoordinates end)
    {
        if (!ValidateDrawRect(start, end))
            return false;
        return true;
    }

    public DrawTool()
    {
        IoCManager.InjectDependencies(this);
    }
}
