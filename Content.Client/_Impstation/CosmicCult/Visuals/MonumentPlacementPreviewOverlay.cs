using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Impstation.CosmicCult.Visuals;

public sealed class MonumentPlacementPreviewOverlay : Overlay
{
    private readonly IEntityManager _entityManager;
    private readonly IPlayerManager _playerManager;
    private readonly SpriteSystem _spriteSystem;
    private readonly SharedMapSystem _mapSystem;
    private readonly MonumentPlacementPreviewSystem _preview;
    private readonly IGameTiming _timing;
    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    private readonly ShaderInstance _saturationShader;
    private readonly ShaderInstance _unshadedShader;
    private readonly ShaderInstance _starsShader;

    public bool LockPlacement = false;
    private EntityCoordinates _lastPos = new();

    //for a slight fade in / out
    //ss14's formatting settings can take my needlessly pulbic variables away from my cold, dead hands
    public float fadeInProgress = 0;
    public float fadeInTime = 0.25f;
    public bool fadingIn = true;

    public float fadeOutProgress = 0;
    public float fadeOutTime = 0.25f;
    public bool fadingOut = false;

    public float alpha = 0;

    private SpriteSpecifier mainTex;
    private SpriteSpecifier outlineTex;
    private SpriteSpecifier starTex;

    //todo arbitrary sprite drawing overlay at some point
    //I don't want to have to make a new overlay for every "draw a sprite at x" thing
    //also I kinda want wrappers around the dog ass existing arbitrary rendering methods

    //evil huge ctor because doing iocmanager stuff was killing the client for some reason
    public MonumentPlacementPreviewOverlay(IEntityManager entityManager, IPlayerManager playerManager, SpriteSystem spriteSystem, SharedMapSystem mapSystem, IPrototypeManager protoMan, MonumentPlacementPreviewSystem preview, IGameTiming timing, int tier)
    {
        _entityManager = entityManager;
        _playerManager = playerManager;
        _spriteSystem = spriteSystem;
        _mapSystem = mapSystem;
        _preview = preview;
        _timing = timing;

        _saturationShader = protoMan.Index<ShaderPrototype>("SaturationShuffle").InstanceUnique();
        _saturationShader.SetParameter("tileSize", new Vector2(96, 96));
        _saturationShader.SetParameter("hsv", new Robust.Shared.Maths.Vector3(1.0f, 0.25f, 0.2f));

        _starsShader = protoMan.Index<ShaderPrototype>("MonumentPulse").InstanceUnique();
        _starsShader.SetParameter("tileSize", new Vector2(96, 96));

        _unshadedShader = protoMan.Index<ShaderPrototype>("unshaded").Instance(); //doesn't need a unique instance

        ZIndex = (int) Shared.DrawDepth.DrawDepth.Mobs; //make the overlay render at the same depth as the actual sprite. might want to make it 1 lower if things get wierd with it.

        //will fuck up if the wrong tier is passed in but it's not my problem if that happens
        mainTex = new SpriteSpecifier.Rsi(new ResPath("_Impstation/CosmicCult/Tileset/monument.rsi"), $"stage{tier}");
        outlineTex = new SpriteSpecifier.Rsi(new ResPath("_Impstation/CosmicCult/Tileset/monument.rsi"), $"stage{tier}-placement-ghost-1");
        starTex = new SpriteSpecifier.Rsi(new ResPath("_Impstation/CosmicCult/Tileset/monument.rsi"), $"stage{tier}-placement-ghost-2");
    }

    //this might get wierd if the player managed to leave the grid they put the monument on? theoretically not a concern because it can't be placed too close to space.
    //shouldn't crash due to the comp checks, though.
    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent<TransformComponent>(_playerManager.LocalEntity, out var transformComp))
            return;

        if (!_entityManager.TryGetComponent<MapGridComponent>(transformComp.GridUid, out var grid))
            return;

        if (!_entityManager.TryGetComponent<TransformComponent>(transformComp.ParentUid, out var parentTransform))
            return;

        var worldHandle = args.WorldHandle;

        //make effects look more nicer
        var time = (float) _timing.FrameTime.TotalSeconds;
        //make the fade in / out progress
        if (fadingIn)
        {
            fadeInProgress += time;
            if (fadeInProgress >= fadeInTime)
            {
                fadingIn = false;
                fadeInProgress = fadeInTime;
            }
            alpha = fadeInProgress / fadeInTime;
        }

        if (fadingOut)
        {
            fadeOutProgress += time;
            if (fadeOutProgress >= fadeOutTime)
            {
                fadingOut = false;
                fadeOutProgress = fadeOutTime;
            }
            alpha = 1 - fadeOutProgress / fadeOutTime;
        }

        //have the outline's alpha slightly "breathe"
        var outlineAlphaModulate = 0.65f + (0.35f * (float) Math.Sin(_timing.CurTime.TotalSeconds));

        //stuff to make the monument preview stick in place once the ability is confirmed
        Color color;
        if (!LockPlacement)
        {
            //set the colour based on if the target tile is valid or not
            color = _preview.VerifyPlacement(transformComp, out var snappedCoords) ? Color.White.WithAlpha(outlineAlphaModulate * alpha) : Color.Gray.WithAlpha(outlineAlphaModulate * 0.5f * alpha);
            _lastPos = snappedCoords; //update the position
        }
        else
        {
            //if the position is locked, then it has to be valid so always use the valid colour
            color = Color.White.WithAlpha(outlineAlphaModulate * alpha);
        }

        worldHandle.SetTransform(parentTransform.LocalMatrix);

        //for the desaturated monument "shadow"
        worldHandle.UseShader(_saturationShader);
        worldHandle.DrawTexture(_spriteSystem.Frame0(mainTex), _lastPos.Position - new Vector2(1.5f, 0.5f), Color.White.WithAlpha(alpha)); //needs the offset to render in the proper position. does not inherit the extra modulate

        //for the outline to pop
        worldHandle.UseShader(_unshadedShader);
        worldHandle.DrawTexture(_spriteSystem.Frame0(outlineTex), _lastPos.Position - new Vector2(1.5f, 0.5f), color);

        //some fancy schmancy things for the inside of the monument
        worldHandle.UseShader(_starsShader);
        worldHandle.DrawTexture(_spriteSystem.Frame0(starTex), _lastPos.Position - new Vector2(1.5f, 0.5f), color.WithAlpha(alpha)); //don't inherit the alpha mult on the inside bit
        worldHandle.UseShader(null);
    }
}
