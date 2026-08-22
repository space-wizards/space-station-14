using System.Linq;
using Content.Client.Administration.Managers;
using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Utility;
using static Content.Shared.Administration.PermissionsEuiMsg;

namespace Content.Client.Administration.UI.Permissions;

[UsedImplicitly]
public sealed partial class PermissionsEui : BaseEui
{
    public const int NoRank = -1;

    private readonly PermissionsWindow _permissionsWindow;
    private readonly List<BaseWindow> _subWindows = new();

    private Dictionary<int, PermissionsEuiState.AdminRankData> _ranks =
        new();

    public PermissionsEui()
    {
        _permissionsWindow = new PermissionsWindow();

        _permissionsWindow.OnAddAdminPressed += AddAdminPressed;
        _permissionsWindow.OnAddAdminRankPressed += AddAdminRankPressed;

        _permissionsWindow.OnEditAdminPressed += OnEditAdminPressed;
        _permissionsWindow.OnEditAdminRankPressed += OnEditRankPressed;
    }

    public override void Opened()
    {
        _permissionsWindow.OnClose += CloseEverything;
        _permissionsWindow.OpenCentered();
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

    private void AddAdminPressed()
    {
        OpenEditWindow(null);
    }

    private void AddAdminRankPressed()
    {
        OpenRankEditWindow(null);
    }

    private void OnEditAdminPressed(PermissionsEuiState.AdminData admin)
    {
        OpenEditWindow(admin);
    }

    private void OnEditRankPressed(KeyValuePair<int, PermissionsEuiState.AdminRankData> rank)
    {
        OpenRankEditWindow(rank);
    }

    private void OpenEditWindow(PermissionsEuiState.AdminData? data)
    {
        var window = new EditAdminWindow();
        window.OnSavePressed += (rankId, title, name, suspended) =>
            SaveAdminPressed(window, rankId, title, name, suspended);

        if (data != null)
        {
            window.OnRemovePressed += () => RemoveButtonPressed(window);
        }

        window.OnClose += () => _subWindows.Remove(window);
        window.SetAdminData(data, _ranks);
        window.OpenCentered();

        _subWindows.Add(window);
    }


    private void OpenRankEditWindow(KeyValuePair<int, PermissionsEuiState.AdminRankData>? rank)
    {
        var window = new EditAdminRankWindow();
        window.OnSavePressed += () => SaveAdminRankPressed(window);

        if (rank != null)
        {
            window.OnRemovePressed += () => RemoveRankButtonPressed(window);
        }

        window.OnClose += () => _subWindows.Remove(window);
        window.SetRankData(rank);
        window.OpenCentered();
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

    private void SaveAdminPressed(EditAdminWindow popup, int? rank, string? titleText, string name, bool suspended)
    {
        popup.CollectSetFlags(out var pos, out var neg);

        if (rank == NoRank)
        {
            rank = null;
        }

        var title = string.IsNullOrWhiteSpace(titleText) ? null : titleText;

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
            DebugTools.AssertNotNull(name);

            SendMessage(new AddAdmin
            {
                UserNameOrId = name,
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

    public override void HandleState(EuiStateBase stateBase)
    {
        if (stateBase is not PermissionsEuiState state
            || state.IsLoading)
        {
            return;
        }

        var orderedAdmins = state.Admins.OrderBy(d => d.UserName);
        _permissionsWindow.UpdateAdmins(orderedAdmins, state.AdminRanks);
        _permissionsWindow.UpdateAdminRanks(state.AdminRanks);
        _ranks = state.AdminRanks;
    }
}
