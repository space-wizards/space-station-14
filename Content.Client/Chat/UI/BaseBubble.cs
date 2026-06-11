using System.Numerics;
using Content.Shared.Speech;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Client.Chat.UI;

public abstract partial class BaseBubble : Control
{
    [Dependency] protected IEyeManager EyeManager = default!;
    [Dependency] protected IEntityManager EntityManager = default!;
    [Dependency] protected IConfigurationManager ConfigManager = default!;
    protected SharedTransformSystem TransformSystem = default!;

    /// <summary>
    ///     The distance in world space to offset the speech bubble from the center of the entity.
    ///     i.e. greater -> higher above the mob's head.
    /// </summary>
    protected const float EntityVerticalOffset = 0.5f;

    public float VerticalOffset { get; set; }
    protected float VerticalOffsetAchieved;

    public Vector2 ContentSize { get; protected set; }

    protected readonly EntityUid SenderEntity;

    protected BaseBubble(EntityUid senderEntity)
    {
        IoCManager.InjectDependencies(this);
        SenderEntity = senderEntity;
        TransformSystem = EntityManager.System<SharedTransformSystem>();
        RectClipContent = true;
    }

    /// <summary>
    ///     Updates positioning and visibility of the bubble.
    /// </summary>
    protected void UpdateBubblePosition(FrameEventArgs args)
    {
        // Lerp to our new vertical offset if it's been modified.
        if (MathHelper.CloseToPercent(VerticalOffsetAchieved - VerticalOffset, 0, 0.1))
        {
            VerticalOffsetAchieved = VerticalOffset;
        }
        else
        {
            VerticalOffsetAchieved = MathHelper.Lerp(VerticalOffsetAchieved, VerticalOffset, 10 * args.DeltaSeconds);
        }

        if (!EntityManager.TryGetComponent<TransformComponent>(SenderEntity, out var xform) || xform.MapID != EyeManager.CurrentEye.Position.MapId)
        {
            Modulate = Color.White.WithAlpha(0);
            return;
        }

        var baseOffset = 0f;

        if (EntityManager.TryGetComponent<SpeechComponent>(SenderEntity, out var speech))
            baseOffset = speech.SpeechBubbleOffset;

        var offset = (-EyeManager.CurrentEye.Rotation).ToWorldVec() * -(EntityVerticalOffset + baseOffset);
        var worldPos = TransformSystem.GetWorldPosition(xform) + offset;

        var lowerCenter = EyeManager.WorldToScreen(worldPos) / UIScale;
        var screenPos = lowerCenter - new Vector2(ContentSize.X / 2, ContentSize.Y + VerticalOffsetAchieved);
        // Round to nearest 0.5
        screenPos = (screenPos * 2).Rounded() / 2;
        LayoutContainer.SetPosition(this, screenPos);

        var height = MathF.Ceiling(MathHelper.Clamp(lowerCenter.Y - screenPos.Y, 0, ContentSize.Y));
        SetHeight = height;
    }
}
