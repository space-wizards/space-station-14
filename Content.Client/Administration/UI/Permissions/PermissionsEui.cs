using System.Linq;
using Content.Client.Administration.Managers;
using Content.Client.Eui;
using Content.Client.Stylesheets;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Utility;
using static Content.Shared.Administration.PermissionsEuiMsg;

namespace Content.Client.Administration.UI.Permissions;

[UsedImplicitly]
public sealed partial class PermissionsEui : BaseEui
{
    [Dependency] private IClientAdminManager _adminManager = default!;

    public const int NoRank = -1;

    private readonly PermissionsWindow _permissionsWindow;
    private readonly List<BaseWindow> _subWindows = new();

    private Dictionary<int, PermissionsEuiState.AdminRankData> _ranks =
        new();

    public PermissionsEui()
    {
        IoCManager.InjectDependencies(this);

        _permissionsWindow = new PermissionsWindow(this);
        _permissionsWindow.AddAdminButton.OnPressed += AddAdminPressed;
        _permissionsWindow.AddAdminRankButton.OnPressed += AddAdminRankPressed;
        _permissionsWindow.OnClose += CloseEverything;
    }

    public override void Closed()
    {
        base.Closed();

        SendMessage(new CloseEuiMessage());
        CloseEverything();
    }

    public Dictionary<int, PermissionsEuiState.AdminRankData> Ranks => _ranks;

    private void CloseEverything()
    {
        foreach (var subWindow in _subWindows.ToArray())
        {
            subWindow.Close();
        }

        _permissionsWindow.Close();
    }

    private void AddAdminPressed(BaseButton.ButtonEventArgs obj)
    {
        OpenEditWindow(null);
    }

    private void AddAdminRankPressed(BaseButton.ButtonEventArgs obj)
    {
        OpenRankEditWindow(null);
    }


    private void OnEditPressed(PermissionsEuiState.AdminData admin)
    {
        OpenEditWindow(admin);
    }

    private void OpenEditWindow(PermissionsEuiState.AdminData? data)
    {
        var window = new EditAdminWindow(this, _adminManager, data);
        window.SaveButton.OnPressed += _ => SaveAdminPressed(window);
        window.OpenCentered();
        window.OnClose += () => _subWindows.Remove(window);
        if (data != null)
        {
            window.RemoveButton!.OnPressed += _ => RemoveButtonPressed(window);
        }

        _subWindows.Add(window);
    }


    private void OpenRankEditWindow(KeyValuePair<int, PermissionsEuiState.AdminRankData>? rank)
    {
        var window = new EditAdminRankWindow(this, _adminManager, rank);
        window.SaveButton.OnPressed += _ => SaveAdminRankPressed(window);
        window.OpenCentered();
        window.OnClose += () => _subWindows.Remove(window);
        if (rank != null)
        {
            window.RemoveButton!.OnPressed += _ => RemoveRankButtonPressed(window);
        }

        _subWindows.Add(window);
    }

    private void RemoveButtonPressed(EditAdminWindow window)
    {
        SendMessage(new RemoveAdmin { UserId = window.SourceData!.Value.UserId });

        window.Close();
    }

    private void RemoveRankButtonPressed(EditAdminRankWindow window)
    {
        SendMessage(new RemoveAdminRank { Id = window.SourceId!.Value });

        window.Close();
    }

    private void SaveAdminPressed(EditAdminWindow popup)
    {
        popup.CollectSetFlags(out var pos, out var neg);

        int? rank = popup.RankButton.SelectedId;
        if (rank == NoRank)
        {
            rank = null;
        }

        var title = string.IsNullOrWhiteSpace(popup.TitleEdit.Text) ? null : popup.TitleEdit.Text;
        var suspended = popup.SuspendedCheckbox.Pressed;

        if (popup.SourceData is { } src)
        {
            SendMessage(new UpdateAdmin
            {
                UserId = src.UserId,
                Title = title,
                PosFlags = pos,
                NegFlags = neg,
                RankId = rank,
                Suspended = suspended,
            });
        }
        else
        {
            DebugTools.AssertNotNull(popup.NameEdit);

            SendMessage(new AddAdmin
            {
                UserNameOrId = popup.NameEdit!.Text,
                Title = title,
                PosFlags = pos,
                NegFlags = neg,
                RankId = rank,
                Suspended = suspended,
            });
        }

        popup.Close();
    }


    private void SaveAdminRankPressed(EditAdminRankWindow popup)
    {
        var flags = popup.CollectSetFlags();
        var name = popup.NameEdit.Text;

        if (popup.SourceId is { } src)
        {
            SendMessage(new UpdateAdminRank
            {
                Id = src,
                Flags = flags,
                Name = name,
            });
        }
        else
        {
            SendMessage(new AddAdminRank
            {
                Flags = flags,
                Name = name
            });
        }

        popup.Close();
    }

    public override void Opened()
    {
        _permissionsWindow.OpenCentered();
    }

    public override void HandleState(EuiStateBase state)
    {
        var s = (PermissionsEuiState)state;

        if (s.IsLoading)
        {
            return;
        }

        _ranks = s.AdminRanks;

        _permissionsWindow.AdminsList.RemoveAllChildren();
        foreach (var admin in s.Admins.OrderBy(d => d.UserName))
        {
            var al = _permissionsWindow.AdminsList;
            var name = admin.UserName ?? admin.UserId.ToString();

            al.AddChild(new Label { Text = name });

            var titleControl = new Label
            {
                Text = admin.Title ?? Loc.GetString("permissions-eui-edit-admin-title-control-text").ToLowerInvariant()
            };
            if (admin.Title == null) // none
            {
                titleControl.StyleClasses.Add(StyleClass.Italic);
            }

            al.AddChild(titleControl);

            bool italic;
            string rank;
            var combinedFlags = admin.PosFlags;
            if (admin.RankId is { } rankId)
            {
                italic = false;
                var rankData = s.AdminRanks[rankId];
                rank = rankData.Name;
                combinedFlags |= rankData.Flags;
            }
            else
            {
                italic = true;
                rank = Loc.GetString("permissions-eui-edit-no-rank-text").ToLowerInvariant();
            }

            var rankControl = new Label { Text = rank };
            if (italic)
            {
                rankControl.StyleClasses.Add(StyleClass.Italic);
            }

            al.AddChild(rankControl);

            var flagsText = AdminFlagsHelper.PosNegFlagsText(admin.PosFlags, admin.NegFlags);

            al.AddChild(new Label
            {
                Text = flagsText,
                HorizontalExpand = true,
                HorizontalAlignment = Control.HAlignment.Center,
            });

            var editButton = new Button { Text = Loc.GetString("permissions-eui-edit-title-button") };
            editButton.OnPressed += _ => OnEditPressed(admin);
            al.AddChild(editButton);

            if (!_adminManager.HasFlag(combinedFlags))
            {
                editButton.Disabled = true;
                editButton.ToolTip = Loc.GetString("permissions-eui-do-not-have-required-flags-to-edit-admin-tooltip");
            }
        }

        _permissionsWindow.AdminRanksList.RemoveAllChildren();
        foreach (var kv in s.AdminRanks)
        {
            var rank = kv.Value;
            var flagsText = string.Join(' ', AdminFlagsHelper.FlagsToNames(rank.Flags).Select(f => $"+{f}"));
            _permissionsWindow.AdminRanksList.AddChild(new Label { Text = rank.Name });
            _permissionsWindow.AdminRanksList.AddChild(new Label
            {
                Text = flagsText,
                HorizontalExpand = true,
                HorizontalAlignment = Control.HAlignment.Center,
            });
            var editButton = new Button { Text = Loc.GetString("permissions-eui-edit-admin-rank-button") };
            editButton.OnPressed += _ => OnEditRankPressed(kv);
            _permissionsWindow.AdminRanksList.AddChild(editButton);

            if (!_adminManager.HasFlag(rank.Flags))
            {
                editButton.Disabled = true;
                editButton.ToolTip = Loc.GetString("permissions-eui-do-not-have-required-flags-to-edit-rank-tooltip");
            }
        }
    }

    private void OnEditRankPressed(KeyValuePair<int, PermissionsEuiState.AdminRankData> rank)
    {
        OpenRankEditWindow(rank);
    }
}
