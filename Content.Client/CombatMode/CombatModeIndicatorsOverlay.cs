using System.Numerics;
using Content.Client.Hands.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Utility;

namespace Content.Client.CombatMode;

/// <summary>
///   This shows something like crosshairs for the combat mode next to the mouse cursor.
///   For weapons with the gun class, a crosshair of one type is displayed,
///   while for all other types of weapons and items in hand, as well as for an empty hand,
///   a crosshair of a different type is displayed. These crosshairs simply show the state of combat mode (on|off).
/// </summary>
public sealed class CombatModeIndicatorsOverlay : Overlay
{
    private readonly IInputManager _inputManager;
    private readonly IEntityManager _entMan;
    private readonly IEyeManager _eye;
    private readonly CombatModeSystem _combat;
    private readonly HandsSystem _hands = default!;

    private readonly Texture _gunSight;
    private readonly Texture _meleeSight;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public Color MainColor = Color.White.WithAlpha(0.3f);
    public Color StrokeColor = Color.Black.WithAlpha(0.5f);
    public float Scale = 0.6f;  // 1 is a little big

    public CombatModeIndicatorsOverlay(IInputManager input, IEntityManager entMan,
            IEyeManager eye, CombatModeSystem combatSys, HandsSystem hands)
    {
        _inputManager = input;
        _entMan = entMan;
        _eye = eye;
        _combat = combatSys;
        _hands = hands;

        var spriteSys = _entMan.EntitySysManager.GetEntitySystem<SpriteSystem>();
        _gunSight = spriteSys.Frame0(new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Misc/crosshair_pointers.rsi"),
            "gun_sight"));
        _meleeSight = spriteSys.Frame0(new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Misc/crosshair_pointers.rsi"),
             "melee_sight"));
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_combat.IsInCombatMode())
            return false;

        return base.BeforeDraw(in args);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var mouseScreenPosition = _inputManager.MouseScreenPosition;
        var mousePosMap = _eye.PixelToMap(mouseScreenPosition);
        if (mousePosMap.MapId != args.MapId)
            return;

        var handEntity = _hands.GetActiveHandEntity();
        var isHandGunItem = _entMan.HasComponent<GunComponent>(handEntity);

        var mousePos = mouseScreenPosition.Position;
        var uiScale = (args.ViewportControl as Control)?.UIScale ?? 1f;
        var limitedScale = uiScale > 1.25f ? 1.25f : uiScale;

        var sight = isHandGunItem ? _gunSight : _meleeSight;
        DrawSight(sight, args.ScreenHandle, mousePos, limitedScale * Scale);
    }

    private void DrawSight(Texture sight, DrawingHandleScreen screen, Vector2 centerPos, float scale)
    {
        var sightSize = sight.Size * scale;
        var expandedSize = sightSize + new Vector2(7f, 7f);

        screen.DrawTextureRect(sight,
            UIBox2.FromDimensions(centerPos - sightSize * 0.5f, sightSize), StrokeColor);
        screen.DrawTextureRect(sight,
            UIBox2.FromDimensions(centerPos - expandedSize * 0.5f, expandedSize), MainColor);
    }
}
