using System.Numerics;
using Content.Client.Administration.UI.BanList.Bans;
using Content.Client.Administration.UI.BanList.RoleBans;
using Content.Client.Eui;
using Content.Shared.Administration.BanList;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Administration.UI.BanList;

[UsedImplicitly]
public sealed class BanListEui : BaseEui
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private BanListIdsPopup? _popup;

    public BanListEui()
    {
        BanWindow = new BanListWindow();
        BanWindow.OnClose += OnClosed;

        BanControl = BanWindow.BanList;
        BanControl.LineIdsClicked += OnLineIdsClicked;

        RoleBanControl = BanWindow.RoleBanList;
        RoleBanControl.LineIdsClicked += OnLineIdsClicked;
    }

    private BanListWindow BanWindow { get; }

    private BanListControl BanControl { get; }
    private RoleBanListControl RoleBanControl { get; }

    private void OnClosed()
    {
        if (_popup != null)
        {
            _popup.Close();
            _popup.Dispose();
            _popup = null;
        }

        SendMessage(new CloseEuiMessage());
    }

    public override void Closed()
    {
        base.Closed();
        BanWindow.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not BanListEuiState s)
            return;

        BanWindow.SetTitlePlayer(s.BanListPlayerName);

        s.Bans.Sort((a, b) => a.BanTime.CompareTo(b.BanTime));
        BanControl.SetBans(s.Bans);
        RoleBanControl.SetRoleBans(s.RoleBans);
    }

    public override void Opened()
    {
        BanWindow.OpenCentered();
    }

    private static string FormatDate(DateTimeOffset date)
    {
        return date.ToString("MM/dd/yyyy h:mm tt");
    }

    public static void SetData<T>(IBanListLine<T> line, SharedServerBan ban) where T : SharedServerBan
    {
        line.Reason.Text = ban.Reason;
        line.BanTime.Text = FormatDate(ban.BanTime);
        line.Expires.Text = ban.ExpirationTime == null
            ? Loc.GetString("ban-list-permanent")
            : FormatDate(ban.ExpirationTime.Value);

        if (ban.Unban is { } unban)
        {
            var unbanned = Loc.GetString("ban-list-unbanned", ("date", FormatDate(unban.UnbanTime)));
            var unbannedBy = unban.UnbanningAdmin == null
                ? string.Empty
                : $"\n{Loc.GetString("ban-list-unbanned-by", ("unbanner", unban.UnbanningAdmin))}";

            line.Expires.Text += $"\n{unbanned}{unbannedBy}";
        }

        line.BanningAdmin.Text = ban.BanningAdminName;
    }

    private void OnLineIdsClicked<T>(IBanListLine<T> line) where T : SharedServerBan
    {
        _popup?.Close();
        _popup = null;

        var ban = line.Ban;
        var id = ban.Id == null ? string.Empty : Loc.GetString("ban-list-id", ("id", ban.Id.Value));
        var ip = ban.Address == null
            ? string.Empty
            : Loc.GetString("ban-list-ip", ("ip", ban.Address.Value.address));
        var hwid = ban.HWId == null ? string.Empty : Loc.GetString("ban-list-hwid", ("hwid", ban.HWId));
        var guid = ban.UserId == null
            ? string.Empty
            : Loc.GetString("ban-list-guid", ("guid", ban.UserId.Value.ToString()));

        _popup = new BanListIdsPopup(id, ip, hwid, guid);

        var box = UIBox2.FromDimensions(_ui.MousePositionScaled.Position, new Vector2(1, 1));
        _popup.Open(box);
    }
}
