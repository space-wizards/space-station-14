using Content.Shared.Administration.Logs;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.Administration.UI.CustomControls;

public sealed class AdminLogLabel : RichTextLabel
{
    private static readonly Color TimestampColor = new(140, 140, 140);
    private static readonly Color MetadataColor = new(120, 120, 120);

    private bool _showMetadata;

    public AdminLogLabel(ref SharedAdminLog log, HSeparator separator, bool showMetadata = false)
    {
        Log = log;
        Separator = separator;

        _showMetadata = showMetadata;

        HorizontalExpand = true;
        SetMessage(BuildMessage(log, _showMetadata));
        OnVisibilityChanged += VisibilityChanged;
    }

    public new SharedAdminLog Log { get; }

    public HSeparator Separator { get; }

    private static FormattedMessage BuildMessage(SharedAdminLog log, bool showMetadata)
    {
        var message = new FormattedMessage();
        message.PushColor(TimestampColor);
        message.AddText($"[{log.Date:HH:mm:ss}] ");
        message.Pop();

        message.AddMarkupPermissive(log.Message);

        if (!showMetadata || log.Entities.Length == 0)
            return message;

        message.AddText("\n");
        message.PushColor(MetadataColor);
        message.AddText("entities: ");

        for (var i = 0; i < log.Entities.Length; i++)
        {
            if (i != 0)
                message.AddText(", ");

            var entity = log.Entities[i];
            var name = entity.EntityName ?? entity.PrototypeId ?? "<unknown>";
            message.AddText($"#{entity.EntityUid} {entity.Role} ({name})");
        }

        message.Pop();

        return message;
    }


    public void SetShowMetadata(bool showMetadata)
    {
        if (_showMetadata == showMetadata)
            return;

        _showMetadata = showMetadata;
        SetMessage(BuildMessage(Log, _showMetadata));
    }

    private void VisibilityChanged(Control control)
    {
        Separator.Visible = Visible;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        OnVisibilityChanged -= VisibilityChanged;
    }
}
