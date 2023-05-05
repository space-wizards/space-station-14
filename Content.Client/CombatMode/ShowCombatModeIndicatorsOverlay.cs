
using Content.Client.Hands.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Utility;

namespace Content.Client.CombatMode;

/// <summary>
///   This shows something like crosshairs for the combat mode next to the mouse cursor.
///   For weapons with the gun class, a crosshair of one type is displayed,
///   while for all other types of weapons and items in hand, as well as for an empty hand,
///   a crosshair of a different type is displayed. These crosshairs simply show the state of combat mode (on|off).
/// </summary>
public sealed class ShowCombatModeIndicatorsOverlay : Overlay
{
    private IInputManager _inputManager;
    private IEntityManager _entMan;
    private IEyeManager _eye;
    private CombatModeSystem _combatSystem;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public Color MainColor = Color.White.WithAlpha(0.3f);
    public Color StrokeColor = Color.Black.WithAlpha(0.5f);
    public float Scale = 0.8f;  // 1 is a little big

    private Texture _gunSight;
    private Texture _meleeSight;
    public ShowCombatModeIndicatorsOverlay(IInputManager input, IEntityManager entMan,
            IEyeManager eye, CombatModeSystem combatSys)
    {
        IoCManager.InjectDependencies(this);

        _inputManager = input;
        _entMan = entMan;
        _eye = eye;
        _combatSystem = combatSys;

        var spriteSys = _entMan.EntitySysManager.GetEntitySystem<SpriteSystem>();
        _gunSight = spriteSys.Frame0(new SpriteSpecifier.Rsi(new($"/Textures/Interface/Misc/crosshair_pointers.rsi"),
            "gun_sight"));
        _meleeSight = spriteSys.Frame0(new SpriteSpecifier.Rsi(new($"/Textures/Interface/Misc/crosshair_pointers.rsi"),
             "melee_sight"));
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_combatSystem.IsInCombatMode())
            return false;

        var mousePosMap = _eye.ScreenToMap(_inputManager.MouseScreenPosition);
        if (mousePosMap.MapId != args.MapId)
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handEntity = _entMan.System<HandsSystem>().GetActiveHandEntity();
        var isHandGunItem = _entMan.HasComponent<GunComponent>(handEntity);

        var screen = args.ScreenHandle;
        var mousePos = _inputManager.MouseScreenPosition.Position;
        var uiScale = (args.ViewportControl as Control)?.UIScale ?? 1f;
        var limitedScale = uiScale > 1.25f ? 1.25f : uiScale;

        var sight = isHandGunItem ? _gunSight : _meleeSight;
        DrawSight(sight, screen, mousePos, limitedScale * Scale);
    }
    private void DrawSight(Texture sight, DrawingHandleScreen screen, Vector2 centerPos, float scale)
    {
        screen.DrawTextureRect(sight,
            GetRectForTexture(sight.Size, centerPos, scale), StrokeColor);
        screen.DrawTextureRect(sight,
            GetRectForTexture(sight.Size, centerPos, scale, 7f), MainColor);
    }

    private static UIBox2 GetRectForTexture(Vector2 textureSize, Vector2 pos, float scale,
        float expandedSize = 0f)
    {
        var sightSize = (textureSize * scale) + expandedSize;
        return UIBox2.FromDimensions(pos - (sightSize / 2), sightSize);
    }
}
