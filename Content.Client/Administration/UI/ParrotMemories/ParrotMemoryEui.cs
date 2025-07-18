using Content.Client.Eui;
using Content.Shared.Administration.ParrotMemories;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.Administration.UI.ParrotMemories;

[UsedImplicitly]
public sealed class ParrotMemoryEui : BaseEui
{
    private ParrotMemoryWindow ParrotMemoryWindow { get; }

    private int _currentRound;

    public ParrotMemoryEui()
    {
        ParrotMemoryWindow = new ParrotMemoryWindow(_currentRound);

        ParrotMemoryWindow.OnOpen += SendRefreshCurrentRound;
        ParrotMemoryWindow.OnClose += () => SendMessage(new CloseEuiMessage());

        ParrotMemoryWindow.CurrentRoundButton.OnPressed += (_) =>
        {
            if (ParrotMemoryWindow.RoundId == _currentRound)
                return;

            // set all lists to dirty
            ParrotMemoryWindow.SetListsDirty();

            // passing null requests the current round. This feels slightly more robust vs using _currentRound
            // because it will for sure get the current round and not whatever _currentRound happens to be
            SendRefresh(null);
            ParrotMemoryWindow.SetRound(_currentRound);
        };

        ParrotMemoryWindow.NextRoundButton.OnPressed += (_) =>
        {
            // set all lists to dirty
            ParrotMemoryWindow.SetListsDirty();

            SendRefresh(ParrotMemoryWindow.RoundId + 1);
            ParrotMemoryWindow.SetRound(ParrotMemoryWindow.RoundId + 1);
        };

        ParrotMemoryWindow.PrevRoundButton.OnPressed += (_) =>
        {
            // set all lists to dirty
            ParrotMemoryWindow.SetListsDirty();

            SendRefresh(ParrotMemoryWindow.RoundId - 1);
            ParrotMemoryWindow.SetRound(ParrotMemoryWindow.RoundId - 1);
        };

        ParrotMemoryWindow.MemoryTabContainer.OnTabChanged += (_) =>
        {
            if (ParrotMemoryWindow.GetActiveList() is not { } parrotMemoryList)
                return;

            // if the list is not dirty, use the number of memory lines to update the count text
            // we won't get a new state in that case
            if (!parrotMemoryList.Dirty)
                ParrotMemoryWindow.UpdateMemoryCountText(parrotMemoryList.MemoryContainer.ChildCount);

            SendRefresh(ParrotMemoryWindow.RoundId);
        };

        ParrotMemoryWindow.RoundLineEdit.OnTextTyped += (_) =>
        {
            // basic validation
            if (!int.TryParse(ParrotMemoryWindow.RoundLineEdit.Text, out var roundId))
            {
                ParrotMemoryWindow.RoundLineEdit.Text = _currentRound.ToString();
                return;
            }

            SendRefresh(roundId);
        };

        ParrotMemoryWindow.RefreshButton.OnPressed += (_) =>
        {
            ParrotMemoryWindow.SetActiveListDirty();
            SendRefresh(ParrotMemoryWindow.RoundId);
        };

        ParrotMemoryWindow.ApplyFilterButton.OnPressed += (_) =>
        {
            ParrotMemoryWindow.SetListsDirty();
            SendRefresh(ParrotMemoryWindow.RoundId);
        };

        ParrotMemoryWindow.ClearFilterButton.OnPressed += (_) =>
        {
            ParrotMemoryWindow.FilterLineEdit.Text = string.Empty;
            ParrotMemoryWindow.SetListsDirty();
            SendRefresh(ParrotMemoryWindow.RoundId);
        };
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

    public override void HandleState(EuiStateBase state)
    {
        base.HandleState(state);

        if (state is not ParrotMemoryEuiState { } parrotMemoryEuiState)
            return;

        _currentRound = parrotMemoryEuiState.CurrentRoundId;

        // in case an admin is going to the next/previous rounds too fast, this should discard state updates that
        // don't match the expected round except for the first time we get a state (current round)
        if (parrotMemoryEuiState.MessagesRoundId != parrotMemoryEuiState.CurrentRoundId && parrotMemoryEuiState.MessagesRoundId != ParrotMemoryWindow.RoundId)
            return;

        // update main ui elements
        ParrotMemoryWindow.SetRound(parrotMemoryEuiState.MessagesRoundId);
        ParrotMemoryWindow.UpdateMemoryCountText(parrotMemoryEuiState.Messages.Count);

        if (ParrotMemoryWindow.GetActiveList() is not { } activeList)
            return;

        // add new ones
        foreach (var message in parrotMemoryEuiState.Messages)
        {
            var memoryLine = new ParrotMemoryLine(message);

            activeList.MemoryContainer.AddChild(memoryLine);

            memoryLine.ParrotBlockButton.OnPressed += (_) =>
            {
                activeList.MemoryContainer.RemoveChild(memoryLine);

                SetMemoryBlocked(message.MessageId, !message.Blocked);

                ParrotMemoryWindow.DecrementMemoryCount(1);

                // set inactive lists to dirty
                ParrotMemoryWindow.SetListsDirty(true);
            };
        }

        activeList.Dirty = false;
    }

    /// <summary>
    /// Sends a refresh message for the active list. Used whenever a tab is switched, refresh is pressed or some other
    /// reason to reload the messages
    /// </summary>
    /// <param name="roundId">ID of the round to request memories for. If set to null will request current round</param>
    private void SendRefresh(int? roundId)
    {
        // never update on fresh lists
        if (ParrotMemoryWindow.GetActiveList() is not { Dirty: true } activeList)
            return;

        // get rid of old messages here so that the UI appears more responsive. UI design is my passion
        activeList.MemoryContainer.RemoveAllChildren();

        SendMessage(new ParrotMemoryRefreshMsg(activeList.ShowBlocked, ParrotMemoryWindow.FilterLineEdit.Text, roundId));
    }

    /// <summary>
    /// Send a refresh message with an undefined round id, returning a state for the current round
    /// </summary>
    private void SendRefreshCurrentRound()
    {
        if (ParrotMemoryWindow.GetActiveList() is not { } activeList)
            return;

        SendMessage(new ParrotMemoryRefreshMsg(activeList.ShowBlocked, ParrotMemoryWindow.FilterLineEdit.Text, null));
    }

    /// <summary>
    /// Set a memory to blocked or unblocked and mark any inactive lists as dirty
    /// </summary>
    /// <param name="messageId">Player message ID of the memory</param>
    /// <param name="block">True to block, false to unblock</param>
    private void SetMemoryBlocked(int messageId, bool block)
    {
        SendMessage(new SetParrotMemoryBlockedMsg(messageId, block));
    }
}
