using System.Numerics;
using Content.Shared.DoAfter;
using Content.Client.UserInterface.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Client.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.Containers;

namespace Content.Client.DoAfter;

public sealed class DoAfterOverlay : Overlay
{
    private readonly IEntityManager _entManager;
    private readonly IGameTiming _timing;
    private readonly IPlayerManager _player;
    private readonly SharedTransformSystem _transform;
    private readonly MetaDataSystem _meta;
    private readonly ProgressColorSystem _progressColor;
    private readonly SharedContainerSystem _container;

    private readonly Texture _barTexture;
    private readonly ShaderInstance _unshadedShader;

    /// <summary>
    ///     Flash time for cancelled DoAfters
    /// </summary>
    private const float FlashTime = 0.125f;

    // Hardcoded width of the progress bar because it doesn't match the texture.
    private const float StartX = 2;
    private const float EndX = 22f;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public DoAfterOverlay(IEntityManager entManager, IPrototypeManager protoManager, IGameTiming timing, IPlayerManager player)
    {
        _entManager = entManager;
        _timing = timing;
        _player = player;
        _transform = _entManager.EntitySysManager.GetEntitySystem<SharedTransformSystem>();
        _meta = _entManager.EntitySysManager.GetEntitySystem<MetaDataSystem>();
        _container = _entManager.EntitySysManager.GetEntitySystem<SharedContainerSystem>();
        _progressColor = _entManager.System<ProgressColorSystem>();
        var sprite = new SpriteSpecifier.Rsi(new("/Textures/Interface/Misc/progress_bar.rsi"), "icon");
        _barTexture = _entManager.EntitySysManager.GetEntitySystem<SpriteSystem>().Frame0(sprite);

        _unshadedShader = protoManager.Index<ShaderPrototype>("unshaded").Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var rotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        // If you use the display UI scale then need to set max(1f, displayscale) because 0 is valid.
        const float scale = 1f;
        var scaleMatrix = Matrix3Helpers.CreateScale(new Vector2(scale, scale));
        var rotationMatrix = Matrix3Helpers.CreateRotation(-rotation);

        var curTime = _timing.CurTime;

        var bounds = args.WorldAABB.Enlarged(5f);
        var localEnt = _player.LocalSession?.AttachedEntity;

        var metaQuery = _entManager.GetEntityQuery<MetaDataComponent>();
        var enumerator = _entManager.AllEntityQueryEnumerator<ActiveDoAfterComponent, DoAfterComponent, SpriteComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out _, out var comp, out var sprite, out var xform))
        {
            if (xform.MapID != args.MapId)
                continue;

            if (comp.DoAfters.Count == 0)
                continue;

            var worldPosition = _transform.GetWorldPosition(xform, xformQuery);
            if (!bounds.Contains(worldPosition))
                continue;

            // shades the do-after bar if the do-after bar belongs to other players
            // does not shade do-afters belonging to the local player
            if (uid != localEnt)
                handle.UseShader(null);
            else
                handle.UseShader(_unshadedShader);

            // If the entity is paused, we will draw the do-after as it was when the entity got paused.
            var meta = metaQuery.GetComponent(uid);
            var time = meta.EntityPaused
                ? curTime - _meta.GetPauseTime(uid, meta)
                : curTime;

            var worldMatrix = Matrix3Helpers.CreateTranslation(worldPosition);
            var scaledWorld = Matrix3x2.Multiply(scaleMatrix, worldMatrix);
            var matty = Matrix3x2.Multiply(rotationMatrix, scaledWorld);
            handle.SetTransform(matty);

            var offset = 0f;

            var isInContainer = _container.IsEntityOrParentInContainer(uid, meta, xform);

            foreach (var doAfter in comp.DoAfters.Values)
            {
                // Hide some DoAfters from other players for stealthy actions (ie: thieving gloves)
                var alpha = 1f;
                if (doAfter.Args.Hidden || isInContainer)
                {
                    if (uid != localEnt)
                        continue;

                    // Hints to the local player that this do-after is not visible to other players.
                    alpha = 0.5f;
                }

                // Use the sprite itself if we know its bounds. This means short or tall sprites don't get overlapped
                // by the bar.
                float yOffset = sprite.Bounds.Height / 2f + 0.05f;

                // Position above the entity (we've already applied the matrix transform to the entity itself)
                // Offset by the texture size for every do_after we have.
                var position = new Vector2(-_barTexture.Width / 2f / EyeManager.PixelsPerMeter,
                    yOffset / scale + offset / EyeManager.PixelsPerMeter * scale);

                // Draw the underlying bar texture
                handle.DrawTexture(_barTexture, position);

                Color color;
                float elapsedRatio;

                // if we're cancelled then flick red / off.
                if (doAfter.CancelledTime != null)
                {
                    var elapsed = doAfter.CancelledTime.Value - doAfter.StartTime;
                    elapsedRatio = (float) Math.Min(1, elapsed.TotalSeconds / doAfter.Args.Delay.TotalSeconds);
                    var cancelElapsed = (time - doAfter.CancelledTime.Value).TotalSeconds;
                    var flash = Math.Floor(cancelElapsed / FlashTime) % 2 == 0;
                    color = GetProgressColor(0, flash ? alpha : 0);
                }
                else
                {
                    var elapsed = time - doAfter.StartTime;
                    elapsedRatio = (float) Math.Min(1, elapsed.TotalSeconds / doAfter.Args.Delay.TotalSeconds);
                    color = GetProgressColor(elapsedRatio, alpha);
                }

                var xProgress = (EndX - StartX) * elapsedRatio + StartX;
                var box = new Box2(new Vector2(StartX, 3f) / EyeManager.PixelsPerMeter, new Vector2(xProgress, 4f) / EyeManager.PixelsPerMeter);
                box = box.Translated(position);
                handle.DrawRect(box, color);
                offset += _barTexture.Height / scale;
            }
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }

    public Color GetProgressColor(float progress, float alpha = 1f)
    {
        return _progressColor.GetProgressColor(progress).WithAlpha(alpha);
    }
}
