using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.Info;

public sealed class ServerInfo : BoxContainer
{
    private readonly RichTextLabel _richTextLabelLeft;
    private readonly RichTextLabel _richTextLabelRight;
    private readonly RichTextLabel _richTextLabelDown;

    public ServerInfo()
    {
        Orientation = LayoutOrientation.Vertical;

        var boxContainer = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            HorizontalAlignment = HAlignment.Left,
            HorizontalExpand = true
        };

        _richTextLabelLeft = new RichTextLabel
        {
            MinWidth = 200
        };

        _richTextLabelRight = new RichTextLabel
        {
            VerticalAlignment = VAlignment.Top
        };

        _richTextLabelDown = new RichTextLabel
        {
            HorizontalAlignment = HAlignment.Left,
            MaxWidth = 500
        };

        AddChild(boxContainer);

        boxContainer.AddChild(_richTextLabelLeft);
        boxContainer.AddChild(_richTextLabelRight);

        AddChild(_richTextLabelDown);
    }
    public void SetInfoBlob(string markup)
    {
        var split = markup.Split("###");
        _richTextLabelLeft.SetMessage(FormattedMessage.FromMarkupOrThrow(split[0]));
        if (split.Length > 1)
            _richTextLabelRight.SetMessage(FormattedMessage.FromMarkupOrThrow(split[1]));
        if (split.Length > 2)
            _richTextLabelDown.SetMessage(FormattedMessage.FromMarkupOrThrow(split[2]));
    }
}
