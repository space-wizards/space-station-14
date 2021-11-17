using Content.Shared.Administration.Logs;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Administration.UI.CustomControls;

public class AdminLogLabel : RichTextLabel
{
    public AdminLogLabel(ref SharedAdminLog log)
    {
        Log = log;
        SetMessage(log.Message);
    }

    public SharedAdminLog Log { get; set; }
}
