using System.Numerics;
using Content.Client.Administration.Managers;
using Content.Client.Stylesheets;
using Content.Shared.Administration;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.Administration.UI.Permissions;

internal sealed class EditAdminWindow : DefaultWindow
{
    public readonly PermissionsEuiState.AdminData? SourceData;
    public readonly LineEdit? NameEdit;
    public readonly LineEdit TitleEdit;
    public readonly OptionButton RankButton;
    public readonly Button SaveButton;
    public readonly Button? RemoveButton;
    public readonly CheckBox SuspendedCheckbox;

    public readonly Dictionary<AdminFlags, (Button inherit, Button sub, Button plus)> FlagButtons
        = new();

    private IClientAdminManager _adminManager;

    public EditAdminWindow(PermissionsEui ui,
        IClientAdminManager adminManager,
        PermissionsEuiState.AdminData? data)
    {
        _adminManager = adminManager;

        MinSize = new Vector2(600, 400);
        SourceData = data;

        Control nameControl;

        if (data is { } dat)
        {
            var name = dat.UserName ?? dat.UserId.ToString();
            Title = Loc.GetString("permissions-eui-edit-admin-window-edit-admin-label",
                ("admin", name));

            nameControl = new Label { Text = name };
        }
        else
        {
            Title = Loc.GetString("permissions-eui-menu-add-admin-button");

            nameControl = NameEdit = new LineEdit { PlaceHolder = Loc.GetString("permissions-eui-edit-admin-window-name-edit-placeholder") };
        }

        TitleEdit = new LineEdit { PlaceHolder = Loc.GetString("permissions-eui-edit-admin-window-title-edit-placeholder") };
        RankButton = new OptionButton();
        SaveButton = new Button { Text = Loc.GetString("permissions-eui-edit-admin-window-save-button"), HorizontalAlignment = HAlignment.Right };

        SuspendedCheckbox = new CheckBox
        {
            Text = Loc.GetString("permissions-eui-edit-admin-window-suspended"),
            Pressed = data?.Suspended ?? false,
        };

        RankButton.AddItem(Loc.GetString("permissions-eui-edit-admin-window-no-rank-button"), PermissionsEui.NoRank);
        foreach (var (rId, rank) in ui.Ranks)
        {
            RankButton.AddItem(rank.Name, rId);
        }

        RankButton.SelectId(data?.RankId ?? PermissionsEui.NoRank);
        RankButton.OnItemSelected += RankSelected;

        var permGrid = new GridContainer
        {
            Columns = 4,
            HSeparationOverride = 0,
            VSeparationOverride = 0
        };

        foreach (var flag in AdminFlagsHelper.AllFlags)
        {
            // Can only grant out perms you also have yourself.
            // Primarily intended to prevent people giving themselves +HOST with +PERMISSIONS but generalized.
            var disable = !_adminManager.HasFlag(flag);
            var flagName = flag.ToString().ToUpper();

            var group = new ButtonGroup();

            var inherit = new Button
            {
                Text = "I",
                StyleClasses = { StyleClass.ButtonOpenRight },
                Disabled = disable,
                Group = group,
            };
            var sub = new Button
            {
                Text = "-",
                StyleClasses = { StyleClass.ButtonOpenBoth },
                Disabled = disable,
                Group = group
            };
            var plus = new Button
            {
                Text = "+",
                StyleClasses = { StyleClass.ButtonOpenLeft },
                Disabled = disable,
                Group = group
            };

            if (data is { } d)
            {
                if ((d.NegFlags & flag) != 0)
                {
                    sub.Pressed = true;
                }
                else if ((d.PosFlags & flag) != 0)
                {
                    plus.Pressed = true;
                }
                else
                {
                    inherit.Pressed = true;
                }
            }
            else
            {
                inherit.Pressed = true;
            }

            permGrid.AddChild(new Label { Text = flagName });
            permGrid.AddChild(inherit);
            permGrid.AddChild(sub);
            permGrid.AddChild(plus);

            FlagButtons.Add(flag, (inherit, sub, plus));
        }

        var bottomButtons = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal
        };
        if (data != null)
        {
            // show remove button.
            RemoveButton = new Button { Text = Loc.GetString("permissions-eui-edit-admin-window-remove-flag-button") };
            bottomButtons.AddChild(RemoveButton);
        }

        bottomButtons.AddChild(SaveButton);

        ContentsContainer.AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Children =
            {
                new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    SeparationOverride = 2,
                    Children =
                    {
                        new BoxContainer
                        {
                            Orientation = BoxContainer.LayoutOrientation.Vertical,
                            HorizontalExpand = true,
                            Children =
                            {
                                nameControl,
                                TitleEdit,
                                RankButton,
                                SuspendedCheckbox,
                            }
                        },
                        permGrid
                    },
                    VerticalExpand = true
                },
                bottomButtons
            }
        });
    }

    private void RankSelected(OptionButton.ItemSelectedEventArgs obj)
    {
        RankButton.SelectId(obj.Id);
    }

    public void CollectSetFlags(out AdminFlags pos, out AdminFlags neg)
    {
        pos = default;
        neg = default;

        foreach (var (flag, (_, s, p)) in FlagButtons)
        {
            if (s.Pressed)
            {
                neg |= flag;
            }
            else if (p.Pressed)
            {
                pos |= flag;
            }
        }
    }
}
