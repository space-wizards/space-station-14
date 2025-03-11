using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.ContentPack;
using Robust.Shared.Enums;
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
    private Vector2 _lastPos = Vector2.Zero;

    //for a slight fade in / out
    public float fadeInProgress = 0;
    public float fadeInTime = 0.25f;
    public bool fadingIn = true;

    public float fadeOutProgress = 0;
    public float fadeOutTime = 0.25f;
    public bool fadingOut = false;

    public float alpha = 0;

    //todo arbitrary sprite drawing overlay at some point
    //I don't want to have to make a new overlay for every "draw a sprite at x" thing
    //also I kinda want wrappers around the dog ass existing arbitrary rendering methods

    //evil huge ctor because doing iocmanager stuff was killing the client for some reason
    public MonumentPlacementPreviewOverlay(IEntityManager entityManager, IPlayerManager playerManager, SpriteSystem spriteSystem, SharedMapSystem mapSystem, IPrototypeManager protoMan, MonumentPlacementPreviewSystem preview, IGameTiming timing)
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

        _starsShader = protoMan.Index<ShaderPrototype>("MonumentPulse").InstanceUnique(); //todo make this shader
        _starsShader.SetParameter("tileSize", new Vector2(96, 96));

        _unshadedShader = protoMan.Index<ShaderPrototype>("unshaded").Instance(); //doesn't need a unique instance

        ZIndex = (int) Shared.DrawDepth.DrawDepth.Mobs; //make the overlay render at the same depth as the actual sprite. might want to make it 1 lower if things get wierd with it.
    }

    //this might get wierd if the player managed to leave the grid they put the monument on? theoretically not a concern because it can't be placed too close to space.
    //shouldn't crash due to the comp checks, though.
    //todo make the overlay fade in / out? that's for the ensaucening later though
    //todo make a shader for this
    //want it to be like, a shadow of the monument w/ an outline & field of stars over it
    //if invalid, the stars are darker, else they twinkle
    //maybe some softly pulsing lines as well?
        //from a base texture etc etc
    //much to think about
    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent<TransformComponent>(_playerManager.LocalEntity, out var transformComp))
            return;

        if (!_entityManager.TryGetComponent<MapGridComponent>(transformComp.GridUid, out var grid))
            return;

        if (!_entityManager.TryGetComponent<TransformComponent>(transformComp.ParentUid, out var parentTransform))
            return;

        //I should really make it not use the raw path but I hate RSIs with a probably unhealthy passion
        //these should probably also be in the ctor instead of here
        var mainTex = new SpriteSpecifier.Texture(new ResPath("_Impstation/CosmicCult/Tileset/monument.rsi/stage1.png"));
        var outlineTex = new SpriteSpecifier.Texture(new ResPath("_Impstation/CosmicCult/Tileset/monument.rsi/stage1-placement-ghost-1.png"));
        var starTex = new SpriteSpecifier.Texture(new ResPath("_Impstation/CosmicCult/Tileset/monument.rsi/stage1-placement-ghost-2-2.png"));
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
            //snap the preview to the tile we'll be spawning the monument on
            //todo repeating myself but copy this over into the monument location validation code as well
            var localTile = _mapSystem.GetTileRef(transformComp.GridUid.Value, grid, transformComp.Coordinates);
            var targetIndices = localTile.GridIndices + new Vector2i(0, 1);
            var snappedCoords = _mapSystem.ToCenterCoordinates(transformComp.GridUid.Value, targetIndices, grid);
            _lastPos = snappedCoords.Position; //update the position

            //set the colour based on if the target tile is valid or not todo make this something else? like a toggle in a shader or so? that's for later anyway
            color = _preview.VerifyPlacement(transformComp) ? Color.White.WithAlpha(outlineAlphaModulate * alpha) : Color.Gray.WithAlpha(outlineAlphaModulate * 0.5f * alpha);
        }
        else
        {
            //if the position is locked, then it has to be valid so always use the valid colour
            color = Color.White.WithAlpha(outlineAlphaModulate * alpha);
        }

        worldHandle.SetTransform(parentTransform.LocalMatrix);

        //for the desaturated monument "shadow"
        worldHandle.UseShader(_saturationShader);
        worldHandle.DrawTexture(_spriteSystem.Frame0(mainTex), _lastPos - new Vector2(1.5f, 0.5f), Color.White.WithAlpha(alpha)); //needs the offset to render in the proper position. does not inherit the extra modulate

        //for the outline to pop
        worldHandle.UseShader(_unshadedShader);
        worldHandle.DrawTexture(_spriteSystem.Frame0(outlineTex), _lastPos - new Vector2(1.5f, 0.5f), color);

        //some fancy schmancy things for the inside of the monument
        worldHandle.UseShader(_starsShader);
        worldHandle.DrawTexture(_spriteSystem.Frame0(starTex), _lastPos - new Vector2(1.5f, 0.5f), color.WithAlpha(alpha)); //don't inherit the alpha mult on the inside bit
        worldHandle.UseShader(null);
    }
}
