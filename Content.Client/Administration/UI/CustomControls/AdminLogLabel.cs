using Content.Shared.Administration.Logs;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Administration.UI.CustomControls;

public sealed class AdminLogLabel : RichTextLabel
{
    public AdminLogLabel(ref SharedAdminLog log, HSeparator separator)
    {
        Log = log;
        Separator = separator;

        SetMessage($"{log.Date:HH:mm:ss}: {log.Message}{FormatEntities(log)}");
        OnVisibilityChanged += VisibilityChanged;
    }

    public new SharedAdminLog Log { get; }

    public HSeparator Separator { get; }

    private static string FormatEntities(SharedAdminLog log)
    {
        if (log.Entities.Length == 0)
            return string.Empty;

        var parts = new string[log.Entities.Length];
        for (var i = 0; i < log.Entities.Length; i++)
        {
            var entity = log.Entities[i];
            var name = entity.EntityName ?? entity.PrototypeId ?? "<unknown>";
            parts[i] = $"#{entity.EntityUid} {entity.Role} ({name})";
        }

        return $" [entities: {string.Join(", ", parts)}]";
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
