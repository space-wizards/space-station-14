using System.Numerics;
using Content.Client.Administration.Managers;
using Content.Shared.Administration;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.Administration.UI.Permissions;

internal sealed class EditAdminRankWindow : DefaultWindow
{
    public readonly int? SourceId;
    public readonly LineEdit NameEdit;
    public readonly Button SaveButton;
    public readonly Button? RemoveButton;
    public readonly Dictionary<AdminFlags, CheckBox> FlagCheckBoxes = new();

    private IClientAdminManager _adminManager;

    public EditAdminRankWindow(PermissionsEui ui,
        IClientAdminManager adminManager,
        KeyValuePair<int, PermissionsEuiState.AdminRankData>? data)
    {
        _adminManager = adminManager;

        Title = Loc.GetString("permissions-eui-edit-admin-rank-window-title");
        MinSize = new Vector2(600, 400);
        SourceId = data?.Key;

        NameEdit = new LineEdit
        {
            PlaceHolder = Loc.GetString("permissions-eui-edit-admin-rank-window-name-edit-placeholder"),
        };

        if (data != null)
        {
            NameEdit.Text = data.Value.Value.Name;
        }

        SaveButton = new Button
        {
            Text = Loc.GetString("permissions-eui-menu-save-admin-rank-button"),
            HorizontalAlignment = HAlignment.Right,
            HorizontalExpand = true,
        };
        var flagsBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical
        };

        foreach (var flag in AdminFlagsHelper.AllFlags)
        {
            // Can only grant out perms you also have yourself.
            // Primarily intended to prevent people giving themselves +HOST with +PERMISSIONS but generalized.
            var disable = !_adminManager.HasFlag(flag);
            var flagName = flag.ToString().ToUpper();

            var checkBox = new CheckBox
            {
                Disabled = disable,
                Text = flagName
            };

            if (data != null && (data.Value.Value.Flags & flag) != 0)
            {
                checkBox.Pressed = true;
            }

            FlagCheckBoxes.Add(flag, checkBox);
            flagsBox.AddChild(checkBox);
        }

        var bottomButtons = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal
        };
        if (data != null)
        {
            // show remove button.
            RemoveButton = new Button { Text = Loc.GetString("permissions-eui-menu-remove-admin-rank-button") };
            bottomButtons.AddChild(RemoveButton);
        }

        bottomButtons.AddChild(SaveButton);

        ContentsContainer.AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Children =
            {
                NameEdit,
                flagsBox,
                bottomButtons
            }
        });
    }

    public AdminFlags CollectSetFlags()
    {
        AdminFlags flags = default;
        foreach (var (flag, chk) in FlagCheckBoxes)
        {
            if (chk.Pressed)
            {
                flags |= flag;
            }
        }

        return flags;
    }
}

