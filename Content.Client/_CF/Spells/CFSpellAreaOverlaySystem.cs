using System.Numerics;
using Content.Client.UserInterface.Systems.Actions;
using Content.Shared.CF.Spells;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;

namespace Content.Client.CF.Spells;

/// <summary>
/// While an area spell is aimed, draws the tiles it will cover. The second click is the normal action target.
/// </summary>
public sealed class CFSpellAreaOverlaySystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlays = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedMapSystem _maps = default!;

    private ActionUIController? _actions;

    public override void Initialize()
    {
        base.Initialize();
        _actions = _ui.GetUIController<ActionUIController>();
        _overlays.AddOverlay(new AreaOverlay(this));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlays.RemoveOverlay<AreaOverlay>();
    }

    private void Draw(in OverlayDrawArgs args)
    {
        if (_actions?.SelectingTargetFor is not { } action)
            return;

        if (!TryComp(action, out CFSpellComponent? spell) || spell.Delivery != CFSpellDelivery.Area || spell.Area == CFSpellAreaKind.None)
            return;

        var player = _player.LocalEntity;
        if (player == null || !TryComp(player, out TransformComponent? xform) || xform.GridUid is not { } grid)
            return;

        if (!TryComp(grid, out MapGridComponent? gridComp))
            return;

        var mouse = _eye.PixelToMap(_input.MouseScreenPosition);
        if (mouse.MapId != args.MapId)
            return;

        var casterMap = _transform.GetMapCoordinates(player.Value, xform);
        if (casterMap.MapId != mouse.MapId)
            return;

        var tiles = CFSpellGrid.Collect(
            spell.Area,
            spell.AreaFeet,
            _maps.WorldToLocal(grid, gridComp, casterMap.Position),
            _maps.WorldToLocal(grid, gridComp, mouse.Position),
            gridComp.TileSize,
            spell.IncludeSelf);

        var color = spell.Friendly
            ? new Color(80, 220, 120, 220)
            : new Color(255, 120, 40, 220);

        foreach (var tile in tiles)
        {
            var center = _maps.LocalToWorld(grid, gridComp, _maps.TileCenterToVector(grid, gridComp, tile));
            var half = gridComp.TileSize * 0.46f;
            var box = new Box2(center - new Vector2(half, half), center + new Vector2(half, half));
            var bl = box.BottomLeft;
            var tr = box.TopRight;
            var br = new Vector2(tr.X, bl.Y);
            var tl = new Vector2(bl.X, tr.Y);
            args.WorldHandle.DrawRect(box, color.WithAlpha(0.22f));
            args.WorldHandle.DrawLine(bl, br, color);
            args.WorldHandle.DrawLine(br, tr, color);
            args.WorldHandle.DrawLine(tr, tl, color);
            args.WorldHandle.DrawLine(tl, bl, color);
        }
    }

    private sealed class AreaOverlay : Overlay
    {
        private readonly CFSpellAreaOverlaySystem _system;
        public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

        public AreaOverlay(CFSpellAreaOverlaySystem system)
        {
            _system = system;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            _system.Draw(args);
        }
    }
}
