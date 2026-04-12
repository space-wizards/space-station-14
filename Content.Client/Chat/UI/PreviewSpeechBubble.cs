using System.Numerics;
using Content.Shared.CCVar;
using Content.Shared.Speech;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Chat.UI;

public sealed class PreviewSpeechBubble : Control
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] protected readonly IConfigurationManager ConfigManager = default!;
    private readonly SharedTransformSystem _transformSystem;


    private readonly EntityUid _senderEntity;

    /// <summary>The text label rendered inside the panel.</summary>
    private readonly RichTextLabel _label;

    /// <summary>The background panel that gives the bubble its visual appearance.</summary>
    private readonly PanelContainer _panel;

    /// <summary>
    ///     The distance in world space to offset the speech bubble from the center of the entity.
    ///     i.e. greater -> higher above the mob's head.
    /// </summary>
    private const float EntityVerticalOffset = 0.5f;

    public float VerticalOffset { get; set; }
    private float _verticalOffsetAchieved;

    public Vector2 ContentSize { get; private set; }

    // MIKEY TODO - missing full fancy text box creation. it sort of works though as it differentiates it now as a preview
    // or is that cope
    public PreviewSpeechBubble(EntityUid senderEntity)
    {
        IoCManager.InjectDependencies(this);
        _senderEntity = senderEntity;
        _transformSystem = _entityManager.System<SharedTransformSystem>();

        RectClipContent = true;

        _label = new RichTextLabel
        {
            MaxWidth = SpeechBubble.SpeechMaxWidth,
        };

        _panel = new PanelContainer
        {
            StyleClasses = { "speechBox", "sayBox" },
            Children = { _label },
            ModulateSelfOverride = Color.White.WithAlpha(ConfigManager.GetCVar(CCVars.SpeechBubbleBackgroundOpacity)),
        };

        AddChild(_panel);
        ForceRunStyleUpdate();
    }

    public float UpdateText(string text)
    {
        var oldHeight = ContentSize.Y;

        var msg = new FormattedMessage();
        msg.AddText(text);
        _label.SetMessage(msg);
        _panel.Measure(Vector2Helpers.Infinity);
        ContentSize = _panel.DesiredSize;

        // If initialized for the first time, make slide-in start position be below the bubble.
        if (oldHeight == 0f)
            _verticalOffsetAchieved = -ContentSize.Y;

        return ContentSize.Y - oldHeight;
    }

    // thanks ai
    // can i just steal this from speechbubble? hm
    // most of this seems duplicative
    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_entityManager.Deleted(_senderEntity))
        {
            Modulate = Color.White.WithAlpha(0);
            return;
        }

        if (MathHelper.CloseToPercent(_verticalOffsetAchieved - VerticalOffset, 0, 0.1))
            _verticalOffsetAchieved = VerticalOffset;
        else
            _verticalOffsetAchieved = MathHelper.Lerp(_verticalOffsetAchieved, VerticalOffset, 10 * args.DeltaSeconds);

        if (!_entityManager.TryGetComponent<TransformComponent>(_senderEntity, out var xform)
            || xform.MapID != _eyeManager.CurrentEye.Position.MapId)
        {
            Modulate = Color.White.WithAlpha(0);
            return;
        }

        Modulate = Color.White;

        var baseOffset = 0f;
        if (_entityManager.TryGetComponent<SpeechComponent>(_senderEntity, out var speech))
            baseOffset = speech.SpeechBubbleOffset;

        var offset = (-_eyeManager.CurrentEye.Rotation).ToWorldVec() * -(EntityVerticalOffset + baseOffset);
        var worldPos = _transformSystem.GetWorldPosition(xform) + offset;

        var lowerCenter = _eyeManager.WorldToScreen(worldPos) / UIScale;

        var screenPos = lowerCenter - new Vector2(ContentSize.X / 2, ContentSize.Y + _verticalOffsetAchieved);

        screenPos = (screenPos * 2).Rounded() / 2;
        LayoutContainer.SetPosition(this, screenPos);

        var height = MathF.Ceiling(MathHelper.Clamp(lowerCenter.Y - screenPos.Y, 0, ContentSize.Y));
        SetHeight = height;
    }
}
