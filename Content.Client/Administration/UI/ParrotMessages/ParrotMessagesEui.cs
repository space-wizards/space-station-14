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

        ParrotMessageWindow.MessageTabContainer.OnTabChanged += (_) => InitializeMessageList();
    }

    private void InitializeMessageList()
    {
        // send a refresh if the list that is currently shown was never refreshed
        if (ParrotMessageWindow.GetActiveList() is not { Initialized: false } parrotMessageList)
            return;

        parrotMessageList.ParrotMessageRefreshButton.OnPressed += (_) =>
        {
            SendMessage(new ParrotMessageRefreshMsg(parrotMessageList.ShowBlocked));
        };

        SendMessage(new ParrotMessageRefreshMsg(parrotMessageList.ShowBlocked));
    }

    public void ChangeMessageBlock(int messageId, bool block)
    {
        SendMessage(new ParrotMessageBlockChangeMsg(messageId, block));
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
