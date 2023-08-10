using Content.Client.Eui;
using Content.Shared.Administration.Notes;
using Content.Shared.Eui;
using JetBrains.Annotations;
using static Content.Shared.Administration.Notes.AdminMessageEuiMsg;

namespace Content.Client.Administration.UI.AdminRemarks;

[UsedImplicitly]
public sealed class AdminMessageEui : BaseEui
{
    private readonly AdminMessagePopupWindow _popup;

    public AdminMessageEui()
    {
        _popup = new AdminMessagePopupWindow();
        _popup.OnAcceptPressed += () => SendMessage(new Accept());
        _popup.OnDismissPressed += () => SendMessage(new Dismiss());
        _popup.OnClose += () => SendMessage(new CloseEuiMessage());
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not AdminMessageEuiState s)
        {
            return;
        }

        _popup.SetMessage(s.Message);
        _popup.SetDetails(s.AdminName, s.AddedOn);
        _popup.Timer = s.Time;
    }

    public override void Opened()
    {
        _popup.OpenCentered();
    }
}
