using Content.Client.Eui;
using Content.Shared.Administration.ParrotMessages;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.Administration.UI.ParrotMessages;

[UsedImplicitly]
public sealed class ParrotMessagesEui : BaseEui
{
    private ParrotMessagesWindow ParrotMessageWindow { get; }

    public ParrotMessagesEui()
    {
        ParrotMessageWindow = new ParrotMessagesWindow();

        ParrotMessageWindow.OnOpen += InitializeMessageList;
        ParrotMessageWindow.OnClose += () => SendMessage(new CloseEuiMessage());

        ParrotMessageWindow.MessageTabContainer.OnTabChanged += (_) => RefreshMessageList();
    }

    private void InitializeMessageList()
    {
        // add event handlers for list controls if this was never initialized
        if (ParrotMessageWindow.GetActiveList() is not { Initialized: false } parrotMessageList)
            return;

        parrotMessageList.RefreshButton.OnPressed += (_) => SendRefresh(parrotMessageList);
        parrotMessageList.CurrentRoundOnly.OnToggled += (_) => SendRefresh(parrotMessageList);
        parrotMessageList.ApplyFilterButton.OnPressed += (_) => SendFilterChange(parrotMessageList);
        parrotMessageList.ClearFilterButton.OnPressed += (_) => ClearFilter(parrotMessageList);

        SendRefresh(parrotMessageList);
    }

    private void RefreshMessageList()
    {
        // add event handlers for list controls if this was never initialized
        if (ParrotMessageWindow.GetActiveList() is not { Dirty: true } parrotMessageList)
            return;

        SendRefresh(parrotMessageList);
    }

    private void SendRefresh(ParrotMessageList parrotMessageList)
    {
        SendMessage(new ParrotMessageRefreshMsg(
            parrotMessageList.ShowBlocked,
            parrotMessageList.CurrentRoundOnly.Pressed
        ));
    }

    private void SendFilterChange(ParrotMessageList parrotMessageList)
    {
        SendMessage(new ParrotMessageFilterChangeMsg(
            parrotMessageList.FilterLineEdit.Text
        ));
    }

    private void ClearFilter(ParrotMessageList parrotMessageList)
    {
        parrotMessageList.FilterLineEdit.Text = "";
        SendFilterChange(parrotMessageList);
    }

    public void ChangeMessageBlock(int messageId, bool block)
    {
        SendMessage(new ParrotMessageBlockChangeMsg(messageId, block));
        ParrotMessageWindow.MarkInactiveListsDirty();
    }

    public override void HandleState(EuiStateBase state)
    {
        base.HandleState(state);

        if (state is not ParrotMessagesEuiState { } parrotState)
            return;

        ParrotMessageWindow.UpdateMessages(this, parrotState);
    }

    public override void Opened()
    {
        ParrotMessageWindow.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        ParrotMessageWindow.Close();
    }
}
