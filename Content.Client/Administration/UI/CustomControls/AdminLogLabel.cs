using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.Administration.UI.CustomControls;

public sealed class AdminLogLabel : RichTextLabel
{
    private static readonly Color TimestampColor = new(140, 140, 140);
    private static readonly Color ServerNameColor = new(100, 140, 180);
    private static readonly Color MetadataColor = new(120, 120, 120);
    private static readonly Color CondensedBadgeColor = new(180, 140, 60);

    /// <summary>
    /// LogTypes that represent condensed/burst events. Used to visually distinguish
    /// them from individual log entries. Looks fine atm, but could be better
    /// </summary>
    private static readonly HashSet<LogType> BurstTypes = new()
    {
        LogType.CombatModeToggleBurst,
        LogType.InteractionRepeatBurst,
        LogType.MeleeMissBurst,
    };

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

    /// <summary>
    /// Returns true if this log entry represents a condensed/burst event.
    /// </summary>
    public bool IsCondensed => BurstTypes.Contains(Log.Type) || Log.Message.StartsWith("[×");

    private static FormattedMessage BuildMessage(SharedAdminLog log, bool showMetadata)
    {
        var message = new FormattedMessage();
        var isCondensed = BurstTypes.Contains(log.Type) || log.Message.StartsWith("[×");

        // Timestamp
        message.PushColor(TimestampColor);
        message.AddText($"[{log.Date:HH:mm:ss}] ");
        message.Pop();

        // Server name — shown when available and not the default "unknown"
        if (!string.IsNullOrEmpty(log.ServerName) && log.ServerName != "unknown")
        {
            message.PushColor(ServerNameColor);
            message.AddText($"[{log.ServerName}] ");
            message.Pop();
        }

        // Condensed badge — visual indicator for burst/condensed entries
        if (isCondensed)
        {
            message.PushColor(CondensedBadgeColor);
            message.AddText("● ");
            message.Pop();
        }

        message.AddMarkupPermissive(log.Message);

        if (!showMetadata)
            return message;

        // Entity metadata — list all participating entities with their role and name.
        if (log.Entities.Length > 0)
        {
            message.AddText("\n");
            message.PushColor(MetadataColor);
            message.AddText("  entities: ");

            for (var i = 0; i < log.Entities.Length; i++)
            {
                if (i != 0)
                    message.AddText(", ");

                var entity = log.Entities[i];
                var name = entity.EntityName ?? entity.PrototypeId ?? "<unknown>";
                message.AddText($"#{entity.EntityUid} {entity.Role} ({name})");
            }

            message.Pop();
        }

        // Type / impact / server line — shown for every log in metadata mode so admins
        // can quickly see log provenance without needing to look at the filter panel.
        message.AddText("\n");
        message.PushColor(MetadataColor);
        message.AddText($"  type: {log.Type} | impact: {log.Impact} | server: {log.ServerName}");
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
