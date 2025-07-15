using Content.Client.Eui;
using Content.Shared.Administration.ParrotMemories;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.Administration.UI.ParrotMemories;

[UsedImplicitly]
public sealed class ParrotMemoryEui : BaseEui
{
    private ParrotMemoryWindow ParrotMemoryWindow { get; }

    public ParrotMemoryEui()
    {
        ParrotMemoryWindow = new ParrotMemoryWindow();

        ParrotMemoryWindow.OnOpen += InitializeMemoryList;
        ParrotMemoryWindow.OnClose += () => SendMessage(new CloseEuiMessage());

        ParrotMemoryWindow.MemoryTabContainer.OnTabChanged += (_) => RefreshMemoryList();
    }

    private void InitializeMemoryList()
    {
        // add event handlers for list controls if this was never initialized
        if (ParrotMemoryWindow.GetActiveList() is not { Initialized: false } parrotMemoryList)
            return;

        parrotMemoryList.RefreshButton.OnPressed += (_) => SendRefresh(parrotMemoryList);
        parrotMemoryList.CurrentRoundOnly.OnToggled += (_) => SendRefresh(parrotMemoryList);
        parrotMemoryList.ApplyFilterButton.OnPressed += (_) => SendRefresh(parrotMemoryList);
        parrotMemoryList.ClearFilterButton.OnPressed += (_) => ClearFilter(parrotMemoryList);

        SendRefresh(parrotMemoryList);
    }

    private void RefreshMemoryList()
    {
        // add event handlers for list controls if this was never initialized
        if (ParrotMemoryWindow.GetActiveList() is not { Dirty: true } parrotMemoryList)
            return;

        SendRefresh(parrotMemoryList);
    }

    private void SendRefresh(ParrotMemoryList parrotMemoryList)
    {
        SendMessage(new ParrotMemoryRefreshMsg(
            parrotMemoryList.ShowBlocked,
            parrotMemoryList.CurrentRoundOnly.Pressed,
            parrotMemoryList.FilterLineEdit.Text
        ));
    }

    private void ClearFilter(ParrotMemoryList parrotMemoryList)
    {
        parrotMemoryList.FilterLineEdit.Text = "";
        SendRefresh(parrotMemoryList);
    }

    public void SetMemoryBlocked(int messageId, bool block)
    {
        SendMessage(new SetParrotMemoryBlockedMsg(messageId, block));
        ParrotMemoryWindow.MarkInactiveListsDirty();
    }

    public override void HandleState(EuiStateBase state)
    {
        base.HandleState(state);

        if (state is not ParrotMemoryEuiState { } parrotState)
            return;

        ParrotMemoryWindow.UpdateMemories(this, parrotState);
    }

    public override void Opened()
    {
        ParrotMemoryWindow.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        ParrotMemoryWindow.Close();
    }
}
