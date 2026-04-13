using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Administration.UI.AuditLogs;

public sealed partial class AdminAuditLogsWindow : DefaultWindow
{
    public AdminAuditLogsControl AuditLogs { get; }

    public AdminAuditLogsWindow()
    {
        RobustXamlLoader.Load(this);
        AuditLogs = FindControl<AdminAuditLogsControl>("AuditLogs");
    }
}
