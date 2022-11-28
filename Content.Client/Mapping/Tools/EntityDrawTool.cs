using System.Linq;
using Content.Client.Mapping.Tools.Widgets;
using Content.Shared.Mapping;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Content.Client.Mapping.Tools;

public sealed class EntityDrawTool : DrawingLikeTool
{
    [Dependency] private readonly IUserInterfaceManager _userInterface = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IEntitySystemManager _entSys = default!;
    [Dependency] private readonly IEntityNetworkManager _entNet = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly ISerializationManager _ser = default!;
    [Dependency] private readonly IResourceCache _cache = default!;

    private EntityUid? _currentPreviewEntity = EntityUid.Invalid;
    private string _lastPreviewProto = string.Empty;

    // This should all be rewritten at some future date.
    // Yes, that's the absolute worst line you could ever read in someone's PR, I'm sorry.
    private void UpdatePreviewEntity()
    {
        // [*ILLEGAL MAGIC PASS*] <- redeeming this, this is currently required to render an entity into the UI.
        // Doing this in a system would be odd so it goes here.
        var protoId = GetConfig().Prototype;
        if (_lastPreviewProto == protoId)
            return;
        if (!_entMan.Deleted(_currentPreviewEntity))
            _entMan.DeleteEntity(_currentPreviewEntity.Value);
        if (protoId == string.Empty)
        {
            _currentPreviewEntity = null;
            _lastPreviewProto = string.Empty;
            return;
        }

        var newEnt = _entMan.SpawnEntity(null, MapCoordinates.Nullspace);
        _currentPreviewEntity = newEnt;

        _lastPreviewProto = protoId;
        var proto = _proto.Index<EntityPrototype>(protoId);


        var component = (Component) _compFactory.GetComponent(typeof(SpriteComponent));
        component.Owner = newEnt;
        _entMan.AddComponent(newEnt, component);
        var newSc = (SpriteComponent)component;
        newSc.Visible = true; // Force visibility.
        /*
        var registration = _compFactory.GetRegistration(typeof(SpriteComponent));


        // the fact this doesn't work makes me incredibly sad.
        //var temp = (object) component;
        //_ser.Copy(toCopy, ref temp);
        _ser.Read(registration.Type, proto.Components[_compFactory.GetComponentName(typeof(SpriteComponent))].Mapping,
            value: component);
        _entMan.AddComponent(newEnt, component);
        */
        var tex = SpriteComponent.GetPrototypeTextures(proto, _cache, out var noRot);
        newSc.NoRotation = noRot;

        foreach (var texture in tex)
        {
            if (texture is RSI.State rsi)
            {
                newSc.AddLayer(rsi.StateId, rsi.RSI);
            }
            else
            {
                // Fallback
                newSc.AddLayer(texture.Default);
            }
        }
    }

    public override SpriteSpecifier ToolActivityIcon =>
        new SpriteSpecifier.Texture(new ResourcePath("/Textures/Mapping/gray_checker.png"));

    public override Type ToolConfigurationControl => typeof(DrawToolWidget);

    private DrawToolWidget GetConfig()
    {
        return _userInterface.ActiveScreen!.GetWidget<DrawToolWidget>()!;
    }

    protected override bool ValidateNewInitialPoint(EntityCoordinates @new)
    {
        if (GetConfig().SnappingMode is { } mode)
        {
            return mode.ValidateNewInitialPoint(InitialClickPoint!.Value, @new);
        }

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

    private void DrawPreviewEnt(EntityCoordinates loc, in OverlayDrawArgs args, IDrawingLikeToolConfiguration cfg, Color color)
    {
        if (_currentPreviewEntity is not { } pe)
            return;

        var sc = _entMan.GetComponent<SpriteComponent>(pe);
        var worldPos = loc.ToMapPos(_entMan);
        var worldRot = _entMan.GetComponent<TransformComponent>(loc.EntityId).WorldRotation + Angle.FromDegrees(cfg.Rotation);
        // this is not fucking okay :'(
        sc.Color = color;
        sc.Render(args.WorldHandle, _eye.CurrentEye.Rotation, worldRot, worldPos);
    }

    public override void PreviewDrawPoint(EntityCoordinates point, in OverlayDrawArgs args)
    {
        var config = GetConfig();
        UpdatePreviewEntity();
        DrawPreviewEnt(point, args, config, Color.Green.WithAlpha(0.80f));
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
