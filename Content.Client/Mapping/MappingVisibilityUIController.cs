using Content.Client.Decals;
using Content.Client.Markers;
using Content.Client.SubFloor;
using Content.Shared.Atmos.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Tag;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.Mapping;

public sealed class MappingVisibilityUIController : UIController
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly ILightManager _lightManager = default!;

    private MappingVisibilityWindow? _window;

    [ValidatePrototypeId<TagPrototype>]
    private const string WallTag = "Wall";

    [ValidatePrototypeId<TagPrototype>]
    private const string CableTag = "Cable";

    [ValidatePrototypeId<TagPrototype>]
    private const string DisposalTag = "Disposal";

    public void ToggleWindow()
    {
        EnsureWindow();

        if (_window!.IsOpen)
        {
            _window.Close();
        }
        else
        {
            _window.Open();
        }
    }

    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;

        _window = UIManager.CreateWindow<MappingVisibilityWindow>();

        _window.Light.Pressed = _lightManager.Enabled;
        _window.Light.OnPressed += args => _lightManager.Enabled = args.Button.Pressed;

        _window.Fov.Pressed = _eyeManager.CurrentEye.DrawFov;
        _window.Fov.OnPressed += args => _eyeManager.CurrentEye.DrawFov = args.Button.Pressed;

        _window.Shadows.Pressed = _lightManager.DrawShadows;
        _window.Shadows.OnPressed += args => _lightManager.DrawShadows = args.Button.Pressed;

        _window.Entities.Pressed = true;
        _window.Entities.OnPressed += OnToggleEntitiesPressed;

        _window.Markers.Pressed = _entitySystemManager.GetEntitySystem<MarkerSystem>().MarkersVisible;
        _window.Markers.OnPressed += args =>
        {
            _entitySystemManager.GetEntitySystem<MarkerSystem>().MarkersVisible = args.Button.Pressed;
        };

        _window.Walls.Pressed = true;
        _window.Walls.OnPressed += args => ToggleWithTag(args, WallTag);

        _window.Airlocks.Pressed = true;
        _window.Airlocks.OnPressed += ToggleWithComp<AirlockComponent>;

        _window.Decals.Pressed = true;
        _window.Decals.OnPressed += _ =>
        {
            _entitySystemManager.GetEntitySystem<DecalSystem>().ToggleOverlay();
        };

        _window.SubFloor.Pressed = _entitySystemManager.GetEntitySystem<SubFloorHideSystem>().ShowAll;
        _window.SubFloor.OnPressed += OnToggleSubfloorPressed;

        _window.Cables.Pressed = true;
        _window.Cables.OnPressed += args => ToggleWithTag(args, CableTag);

        _window.Disposal.Pressed = true;
        _window.Disposal.OnPressed += args => ToggleWithTag(args, DisposalTag);

        _window.Atmos.Pressed = true;
        _window.Atmos.OnPressed += ToggleWithComp<PipeAppearanceComponent>;

        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterTop);
    }

    private void OnToggleEntitiesPressed(BaseButton.ButtonEventArgs args)
    {
        var query = _entityManager.AllEntityQueryEnumerator<SpriteComponent>();

        if (args.Button.Pressed && _window != null)
        {
            _window.Markers.Pressed = true;
            _window.Walls.Pressed = true;
            _window.Airlocks.Pressed = true;
        }
        else if (_window != null)
        {
            _window.Markers.Pressed = false;
            _window.Walls.Pressed = false;
            _window.Airlocks.Pressed = false;
        }

        while (query.MoveNext(out _, out var sprite))
        {
            sprite.Visible = args.Button.Pressed;
        }
    }

    private void OnToggleSubfloorPressed(BaseButton.ButtonEventArgs args)
    {
        _entitySystemManager.GetEntitySystem<SubFloorHideSystem>().ShowAll = args.Button.Pressed;

        if (args.Button.Pressed && _window != null)
        {
            _window.Cables.Pressed = true;
            _window.Atmos.Pressed = true;
            _window.Disposal.Pressed = true;
        }
    }

    private void ToggleWithComp<TComp>(BaseButton.ButtonEventArgs args) where TComp : IComponent
    {
        var query = _entityManager.AllEntityQueryEnumerator<TComp, SpriteComponent>();

        while (query.MoveNext(out _, out _, out var sprite))
        {
            sprite.Visible = args.Button.Pressed;
        }
    }

    private void ToggleWithTag(BaseButton.ButtonEventArgs args, ProtoId<TagPrototype> tag)
    {
        var query = _entityManager.AllEntityQueryEnumerator<TagComponent, SpriteComponent>();
        var tagSystem = _entityManager.EntitySysManager.GetEntitySystem<TagSystem>();

        while (query.MoveNext(out var uid, out _, out var sprite))
        {
            if (tagSystem.HasTag(uid, tag))
                sprite.Visible = args.Button.Pressed;
        }
    }
}
