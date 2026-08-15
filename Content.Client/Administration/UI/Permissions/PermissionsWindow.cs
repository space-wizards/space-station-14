using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.Administration.UI.Permissions;

internal sealed class PermissionsWindow : DefaultWindow
{
    private readonly PermissionsEui _ui;
    public readonly GridContainer AdminsList;
    public readonly GridContainer AdminRanksList;
    public readonly Button AddAdminButton;
    public readonly Button AddAdminRankButton;

    public PermissionsWindow(PermissionsEui ui)
    {
        _ui = ui;
        Title = Loc.GetString("permissions-eui-menu-title");

        var tab = new TabContainer();

        AddAdminButton = new Button
        {
            Text = Loc.GetString("permissions-eui-menu-add-admin-button"),
            HorizontalAlignment = HAlignment.Right
        };

        AddAdminRankButton = new Button
        {
            Text = Loc.GetString("permissions-eui-menu-add-admin-rank-button"),
            HorizontalAlignment = HAlignment.Right
        };

        AdminsList = new GridContainer { Columns = 5, VerticalExpand = true };
        var adminVBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Children = { new ScrollContainer() { VerticalExpand = true, Children = { AdminsList } }, AddAdminButton },
        };
        TabContainer.SetTabTitle(adminVBox, Loc.GetString("permissions-eui-menu-admins-tab-title"));

        AdminRanksList = new GridContainer { Columns = 3, VerticalExpand = true };
        var rankVBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Children = { new ScrollContainer() { VerticalExpand = true, Children = { AdminRanksList } }, AddAdminRankButton }
        };
        TabContainer.SetTabTitle(rankVBox, Loc.GetString("permissions-eui-menu-admin-ranks-tab-title"));

        tab.AddChild(adminVBox);
        tab.AddChild(rankVBox);

        ContentsContainer.AddChild(tab);
        ContentsContainer.MinSize = new(600, 400);
    }
}
