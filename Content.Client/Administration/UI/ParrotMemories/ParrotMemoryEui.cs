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

        ParrotMemoryWindow.OnOpen += InitializeMemoryLists;
        ParrotMemoryWindow.OnClose += () => SendMessage(new CloseEuiMessage());

        ParrotMemoryWindow.MemoryTabContainer.OnTabChanged += (_) => RefreshMemoryList();
    }

    /// <summary>
    /// Function called when the parrot memory window is opened. This initializes
    /// </summary>
    private void InitializeMemoryLists()
    {
        // add event handlers for list controls if they were never initialized
        foreach (var child in ParrotMemoryWindow.MemoryTabContainer.Children)
        {
            if (child is not ParrotMemoryList { Initialized: false } parrotMemoryList)
                return;

            parrotMemoryList.RefreshButton.OnPressed += (_) => SendRefresh(parrotMemoryList);
            parrotMemoryList.CurrentRoundOnly.OnToggled += (_) => SendRefresh(parrotMemoryList);
            parrotMemoryList.ApplyFilterButton.OnPressed += (_) => SendRefresh(parrotMemoryList);
            parrotMemoryList.ClearFilterButton.OnPressed += (_) => ClearFilter(parrotMemoryList);
        }

        if (ParrotMemoryWindow.GetActiveList() is not { } activeList)
            return;

        SendRefresh(activeList);
    }

    /// <summary>
    /// Send a refresh for the active memory list, re-loading the messages
    /// </summary>
    private void RefreshMemoryList()
    {
        // add event handlers for list controls if this was never initialized
        if (ParrotMemoryWindow.GetActiveList() is not { Dirty: true } parrotMemoryList)
            return;

        SendRefresh(parrotMemoryList);
    }

    /// <summary>
    /// Send a refresh message to the server to get a new state
    /// </summary>
    public void SendRefresh(ParrotMemoryList parrotMemoryList)
    {
        SendMessage(new ParrotMemoryRefreshMsg(
            parrotMemoryList.ShowBlocked,
            parrotMemoryList.CurrentRoundOnly.Pressed,
            parrotMemoryList.FilterLineEdit.Text
        ));
    }

    /// <summary>
    /// Clear the text filter on a list and send a refresh
    /// </summary>
    public void ClearFilter(ParrotMemoryList parrotMemoryList)
    {
        parrotMemoryList.FilterLineEdit.Text = "";
        SendRefresh(parrotMemoryList);
    }

    /// <summary>
    /// Set a memory to blocked or unblocked and mark any inactive lists as dirty
    /// </summary>
    /// <param name="messageId">Player message ID of the memory</param>
    /// <param name="block">True to block, false to unblock</param>
    public void SetMemoryBlocked(int messageId, bool block)
    {
        SendMessage(new SetParrotMemoryBlockedMsg(messageId, block));
        ParrotMemoryWindow.MarkInactiveListsDirty();
    }

    public override void HandleState(EuiStateBase state)
    {
        base.HandleState(state);

        if (state is not ParrotMemoryEuiState { } memoryState)
            return;

        var activeList = ParrotMemoryWindow.GetActiveList();

        if (activeList is null)
            return;

        UpdateMemoryList(activeList, memoryState);
    }

    // updates a memory list with new entries
    private void UpdateMemoryList(ParrotMemoryList memoryList, ParrotMemoryEuiState memoryState)
    {
        memoryList.UpdateMemoryCountText(memoryState.Messages.Count);

        // remove all entries from this list
        memoryList.MemoryContainer.RemoveAllChildren();

        foreach (var message in memoryState.Messages)
        {
            var memoryLine = new ParrotMemoryLine(message, memoryState.RoundId);

            memoryList.MemoryContainer.AddChild(memoryLine);

            // we have to dig into the control here to add blocking the memory and removing it from this list
            memoryLine.ParrotBlockButton.OnPressed += (_) =>
            {
                SetMemoryBlocked(message.MessageId, !message.Blocked);
                memoryList.MemoryContainer.RemoveChild(memoryLine);
                memoryList.DecrementMemoryCount(1);
            };
        }
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
