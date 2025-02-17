using System.Linq;
using System.Numerics;
using JetBrains.Annotations;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Client.UserInterface.Systems.Radial.Controls;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Systems.Radial;

[UsedImplicitly]
public sealed class RadialUiController : UIController
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private LayoutContainer _radials = default!;
    private List<RadialContainer> _attachedRadials = new();

    public override void Initialize()
    {
        _radials = new LayoutContainer();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
        gameplayStateLoad.OnScreenUnload += OnScreenUnload;
    }

    public void OnScreenLoad()
    {
        _radials.DisposeAllChildren();
        _attachedRadials.Clear();

        var viewportContainer = UIManager.ActiveScreen!.FindControl<LayoutContainer>("ViewportContainer");
        SetRadialsRoot(viewportContainer);
    }

    public void OnScreenUnload()
    {
        _radials.DisposeAllChildren();
        _attachedRadials.Clear();
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        foreach (var radial in _attachedRadials.Cast<RadialContainer>())
        {
            if (_entityManager.Deleted(radial.AttachedEntity))
            {
                radial.Dispose();
                return;
            }

            if (!_entityManager.TryGetComponent<TransformComponent>(radial.AttachedEntity, out var xform) || xform.MapID != _eyeManager.CurrentMap)
            {
                return;
            }

            var localPlayer = _playerManager.LocalPlayer;
            if (localPlayer == null)
                return;

            // Check distance beetween entities
            if (_entityManager.TryGetComponent<TransformComponent>(localPlayer.ControlledEntity, out var myxform))
            {
                var onePoint = xform.WorldPosition;
                var twoPoint = myxform.WorldPosition;
                var distance = (onePoint - twoPoint).Length();

                if (radial.VisionRadius < distance)
                {
                    radial.Dispose();
                    return;
                }
            }

            var offset = (-_eyeManager.CurrentEye.Rotation).ToWorldVec() * -radial.VerticalOffset;
            var worldPos = xform.WorldPosition + offset;

            var lowerCenter = _eyeManager.WorldToScreen(worldPos) / _radials.UIScale;
            var screenPos = lowerCenter - new Vector2(radial.DesiredSize.X / 2, radial.DesiredSize.Y / 2);
            // Round to nearest 0.5
            screenPos = (screenPos * 2).Rounded() / 2;
            LayoutContainer.SetPosition(radial, screenPos);
        }
    }

    /// <summary>
    /// Set root control for radial controls.
    /// </summary>
    /// <param name="root">Some control element</param>
    public void SetRadialsRoot(LayoutContainer root)
    {
        _radials.Orphan();
        root.AddChild(_radials);
        LayoutContainer.SetAnchorPreset(_radials, LayoutContainer.LayoutPreset.Wide);
        _radials.SetPositionLast();
    }

    /// <summary>
    /// Create radial menu. Don't forget to use Open() at last
    /// </summary>
    /// <returns>RadialContainer control</returns>
    public RadialContainer CreateRadialContainer()
    {
        var radial = new RadialContainer();
        _radials.AddChild(radial);

        radial.OnClose += OnRadialClose;
        radial.OnAttached += OnRadialAttached;
        radial.OnDetached += OnRadialDetached;

        return radial;
    }

    private void OnRadialAttached(RadialContainer radial)
    {
        _attachedRadials.Add(radial);
    }

    private void OnRadialDetached(RadialContainer radial)
    {
        if (_attachedRadials.Contains(radial))
            _attachedRadials.Remove(radial);
    }

    private void OnRadialClose(RadialContainer radial)
    {
        radial.AttachedEntity = null;
    }
}
