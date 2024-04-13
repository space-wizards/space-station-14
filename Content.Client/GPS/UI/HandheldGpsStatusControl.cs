using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Input;
using Content.Shared.Tools.Components;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.GPS.UI;

public sealed class HandheldGpsStatusControl : Control
{
    private readonly Entity<HandheldGpsComponent> _parent;
    private readonly RichTextLabel _label;
    private float _accumulator;
    private readonly IInputManager _inputManager;
    private readonly IEntityManager _entityManager;
    private readonly TransformSystem _transformSystem;
    private readonly EntityQuery<TransformComponent> _transformQuery;
    private bool _lastMode;

    public HandheldGpsStatusControl(Entity<HandheldGpsComponent> parent)
    {
        _parent = parent;
        _inputManager = IoCManager.Resolve<IInputManager>();
        _entityManager = IoCManager.Resolve<IEntityManager>();
        var entitySystemManager = IoCManager.Resolve<IEntitySystemManager>();
        _transformSystem = entitySystemManager.GetEntitySystem<TransformSystem>();
        _transformQuery = _entityManager.GetEntityQuery<TransformComponent>();
        _lastMode = parent.Comp.DisplayMode;
        _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
        AddChild(_label);
        UpdateGpsDetails();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        // has the mode been changed? if so, immediately display new coord mode
        if (_parent.Comp.DisplayMode != _lastMode)
        {
            _lastMode = _parent.Comp.DisplayMode;
            _accumulator = 0;
            UpdateGpsDetails();
            return;
        }

        _accumulator += args.DeltaSeconds;

        if (_accumulator < _parent.Comp.UpdateRate)
            return;

        _accumulator -= _parent.Comp.UpdateRate;

        UpdateGpsDetails();
    }

    private void UpdateGpsDetails()
    {
        if (!_entityManager.TryGetComponent<HandheldGpsComponent>(_parent, out var gpsComponent))
        {
            // if the component is gone we outlived our usefulness
            Parent?.RemoveChild(this);
            return;
        }

        string coordsText;
        if (gpsComponent.DisplayMode)
        {
            // station coordinates mode
            // this used trygetgridtileposition before, but it gives us ints that are rounded in a different way than
            // the crew monitor, making the coords off by one. this uses the same approach as suit sensor/crew monitor
            // code, so they should always be identical
            if (_transformQuery.TryGetComponent(_parent, out var gpsTransform) &&
                gpsTransform.GridUid is not null &&
                _transformQuery.TryGetComponent(gpsTransform.GridUid, out var gridTransform))
            {
                var coordinates = new EntityCoordinates(gpsTransform.GridUid.Value,
                    _transformSystem.GetInvWorldMatrix(gridTransform, _transformQuery)
                        .Transform(_transformSystem.GetWorldPosition(gpsTransform, _transformQuery)));
                var x = MathF.Round(coordinates.X);
                var y = MathF.Round(coordinates.Y);
                coordsText = $"({x}, {y})";
            }
            else
            {
                coordsText = Loc.GetString("handheld-gps-coordinates-unknown");
            }
        }
        else
        {
            // space  coordinates mode
            var mapCoordinates = _transformSystem.GetMapCoordinates(_parent);
            var x = MathF.Round(mapCoordinates.X);
            var y = MathF.Round(mapCoordinates.Y);
            coordsText = $"({x}, {y})";
        }

        var keybind = _inputManager.TryGetKeyBinding((ContentKeyFunctions.UseItemInHand), out var binding)
            ? binding.GetKeyString()
            : Loc.GetString("handheld-gps-coordinates-no-bind");

        _label.SetMarkup(Loc.GetString("handheld-gps-coordinates",
            ("mode", gpsComponent.DisplayMode),
            ("coords", coordsText),
            ("keybind", keybind)
        ));
    }
}
