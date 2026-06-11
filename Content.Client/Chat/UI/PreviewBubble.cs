using Content.Shared.CCVar;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Chat.UI;

/// <summary>
/// A preview bubble that, when enabled, is shown while the user is typing a message.
/// </summary>
public sealed class PreviewBubble : BaseBubble
{
    /// <summary>The text label rendered inside the panel.</summary>
    private readonly RichTextLabel _label;

    /// <summary>The background panel that gives the bubble its visual appearance.</summary>
    private readonly PanelContainer _panel;

    public PreviewBubble(EntityUid senderEntity) : base(senderEntity)
    {
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
            VerticalOffsetAchieved = -ContentSize.Y;

        return ContentSize.Y - oldHeight;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (EntityManager.Deleted(SenderEntity))
        {
            Modulate = Color.White.WithAlpha(0);
            return;
        }

        UpdateBubblePosition(args);
    }
}
