using System.Linq;
using System.Numerics;
using Content.Client.Graphics;
using Content.Client.UserInterface.Systems.Chat;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.Chat.SpeechBubble;

public sealed class ViewportBubbleOffset : IDisposable
{
    //Offset achieved, used for lerping
    public readonly Dictionary<NuSpeechBubble, float> Achieved = [];

    public void Dispose()
    {
        Achieved.Clear();
    }
}

public sealed class BubbleLayout
{
    public NuSpeechBubble Bubble = default!;

    //I tried getting this to work with box2 but I couldn't figure it. idk do it yourself
    public float CenterX;
    public float BaselineY;
    public float Width;
    public float Height;

    public float TargetVerticalOffset;
}

public sealed partial class SpeechBubbleOverlay : Overlay
{
    [Dependency] private IUserInterfaceManager _uiManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly ChatUIController _chatUIController;

    private readonly SpriteSystem _sprite;
    private readonly SharedTransformSystem _transform;

    private EntityQuery<SpriteComponent> _spriteQuery;
    private EntityQuery<TransformComponent> _transformQuery;

    private readonly OverlayResourceCache<ViewportBubbleOffset> _offsetCache = new();

    private readonly List<BubbleLayout> _layoutCache = [];

    private readonly HashSet<NuSpeechBubble> _seenBubbles = [];

    private const float VerticalMargin = 2f;

    public SpeechBubbleOverlay(
        SpriteSystem sprite,
        SharedTransformSystem transform,
        EntityQuery<SpriteComponent> spriteQuery,
        EntityQuery<TransformComponent> transformQuery)
    {
        _sprite = sprite;
        _transform = transform;

        _spriteQuery = spriteQuery;
        _transformQuery = transformQuery;

        IoCManager.InjectDependencies(this);

        _chatUIController = _uiManager.GetUIController<ChatUIController>();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        //Frame updating all of the bubbles to handle fade.
        //FrameUpdate on controls only runs if they're parented to something and these aren't
        foreach (var (_, controls) in _chatUIController.NuActiveSpeechBubbles)
        {
            foreach (var control in controls)
            {
                control.Update(args);
            }
        }
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.ViewportControl == null)
            return;

        if (args.Viewport.Eye is not { } eye)
            return;

        var offsetCache = _offsetCache.GetForViewport(args.Viewport, _ => new ViewportBubbleOffset());

        var deltaTime = (float)_timing.FrameTime.TotalSeconds;

        var matrix = args.ViewportControl.GetWorldToScreenMatrix();

        _layoutCache.Clear();

        foreach (var (ent, controls) in _chatUIController.NuActiveSpeechBubbles)
        {
            if (!_transformQuery.TryComp(ent, out var xform))
                continue;

            if (!_spriteQuery.TryComp(ent, out var sprite))
                continue;

            if (xform.MapID != eye.Position.MapId)
                continue;

            //Get sprite bounding box so we can draw at the bottom.
            //Probably a better way to do this, but I want it drawing at the bottom of entity sprites if possible.
            var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform);
            var bounds = _sprite.CalculateBounds((ent, sprite),
                worldPos,
                worldRot,
                eye.Rotation);

            foreach (var control in controls)
            {
                var offset = (-eye.Rotation).ToWorldVec() * (bounds.Box.Extents.Y);
                var offsetWorldPos = worldPos - offset;
                var pos = Vector2.Transform(offsetWorldPos, matrix);

                _layoutCache.Add(new BubbleLayout
                {
                    Bubble = control,
                    CenterX = pos.X,
                    BaselineY = pos.Y,
                    Width = control.ContentSize.X,
                    Height = control.ContentSize.Y,
                });
            }
        }

        ResolveInterleave(_layoutCache);

        _seenBubbles.Clear();
        foreach (var layout in _layoutCache)
        {
            _seenBubbles.Add(layout.Bubble);

            var target = layout.BaselineY - layout.TargetVerticalOffset;
            var achieved = offsetCache.Achieved.GetValueOrDefault(layout.Bubble, target);

            achieved = MathHelper.CloseToPercent(achieved - target, 0, 0.1) ? target : MathHelper.Lerp(achieved, target, 10 * deltaTime);
            offsetCache.Achieved[layout.Bubble] = achieved;

            var bottomY = layout.BaselineY - achieved;
            var drawPos = new Vector2(layout.CenterX - layout.Width / 2f, bottomY - layout.Height);
            _uiManager.RenderControl(args.RenderHandle, layout.Bubble, drawPos.Floored());
        }

        if (offsetCache.Achieved.Count > _seenBubbles.Count)
        {
            foreach (var key in offsetCache.Achieved.Keys.ToList())
            {
                if (!_seenBubbles.Contains(key))
                    offsetCache.Achieved.Remove(key);
            }
        }
    }

    //The magic happens here
    //Most of this came from pure trial and error
    //I'm 100 percent sure why it works but if I touch it slightly it breaks
    private static void ResolveInterleave(List<BubbleLayout> bubbles)
    {
        //sort by spawn time
        bubbles.Sort((a, b) => b.Bubble.SpawnTime.CompareTo(a.Bubble.SpawnTime));

        var placedBubbles = new List<BubbleLayout>();

        foreach (var bubble in bubbles)
        {
            var left= bubble.CenterX - bubble.Width / 2f;
            var right= bubble.CenterX + bubble.Width / 2f;

            var bottom = bubble.BaselineY;

            foreach (var placedBubble in placedBubbles)
            {
                var placedLeft = placedBubble.CenterX - placedBubble.Width / 2f;
                var placedRight = placedBubble.CenterX + placedBubble.Width / 2f;

                if (placedRight <= left || placedLeft >= right)
                    continue;

                var top = bottom - bubble.Height;
                var placedTop = placedBubble.TargetVerticalOffset - placedBubble.Height;
                if (bottom <= placedTop || top >= placedBubble.TargetVerticalOffset)
                    continue;

                bottom = placedTop - VerticalMargin;
            }

            bubble.TargetVerticalOffset = bottom;

            //find next highest bubble
            var idx = placedBubbles.FindIndex(q => q.TargetVerticalOffset < bottom);
            if (idx < 0)
            {
                //we're the highest
                placedBubbles.Add(bubble);
            }
            else
            {
                //put right below the highest
                placedBubbles.Insert(idx, bubble);
            }
        }
    }

}
