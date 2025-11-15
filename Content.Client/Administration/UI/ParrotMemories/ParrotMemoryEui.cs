using Content.Client.Eui;
using Content.Shared.Administration.ParrotMemories;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.Administration.UI.ParrotMemories;

[UsedImplicitly]
public sealed class ParrotMemoryEui : BaseEui
{
    private ParrotMemoryWindow ParrotMemoryWindow { get; }

    /// <summary>
    /// The round currently in play
    /// </summary>
    private int _currentRound;

    public ParrotMemoryEui()
    {
        ParrotMemoryWindow = new ParrotMemoryWindow(_currentRound);

        ParrotMemoryWindow.OnOpen += SendRefreshCurrentRound;
        ParrotMemoryWindow.OnClose += () => SendMessage(new CloseEuiMessage());

        // Handler for the button that navigates to the current round in play
        ParrotMemoryWindow.CurrentRoundButton.OnPressed += (_) =>
        {
            if (ParrotMemoryWindow.RoundId == _currentRound)
                return;

            // passing null requests the current round. This feels slightly more robust vs using _currentRound
            // because it will for sure get the current round and not whatever _currentRound happens to be
            ChangeRound(null, true);
        };

        ParrotMemoryWindow.NextRoundButton.OnPressed += (_) =>
        {
            ChangeRound(ParrotMemoryWindow.RoundId + 1, true);
        };

        ParrotMemoryWindow.PrevRoundButton.OnPressed += (_) =>
        {
            ChangeRound(ParrotMemoryWindow.RoundId - 1, true);
        };

        // Handler for the button to choose a specific round
        ParrotMemoryWindow.GoToRoundButton.OnPressed += (_) =>
        {
            if (!int.TryParse(ParrotMemoryWindow.RoundLineEdit.Text, out var roundId))
                return;

            ChangeRound(roundId, true);
        };

        // Handler for when a different list is selected
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

        ParrotMemoryWindow.RefreshButton.OnPressed += (_) =>
        {
            ChangeRound(ParrotMemoryWindow.RoundId, true);
        };

        ParrotMemoryWindow.ApplyFilterButton.OnPressed += (_) =>
        {
            ChangeRound(ParrotMemoryWindow.RoundId, true);
        };

        ParrotMemoryWindow.ClearFilterButton.OnPressed += (_) =>
        {
            ParrotMemoryWindow.FilterLineEdit.Text = string.Empty;
            ChangeRound(ParrotMemoryWindow.RoundId, true);
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
        // Otherwise, the UI becomes clunky
        if (parrotMemoryEuiState.SelectedRoundId != parrotMemoryEuiState.CurrentRoundId && parrotMemoryEuiState.SelectedRoundId != ParrotMemoryWindow.RoundId)
            return;

        // update main ui elements
        ParrotMemoryWindow.SetRound(parrotMemoryEuiState.SelectedRoundId);
        ParrotMemoryWindow.UpdateMemoryCountText(parrotMemoryEuiState.Memories.Count);

        if (ParrotMemoryWindow.GetActiveList() is not { } activeList)
            return;

        // add new memories
        foreach (var memory in parrotMemoryEuiState.Memories)
        {
            var memoryLine = new ParrotMemoryLine(memory);

            activeList.MemoryContainer.AddChild(memoryLine);

            memoryLine.ParrotBlockButton.OnPressed += (_) =>
            {
                activeList.MemoryContainer.RemoveChild(memoryLine);

                SetMemoryBlocked(memory.Id, !memory.Blocked);

                ParrotMemoryWindow.DecrementMemoryCount(1);

                // set inactive lists to dirty
                ParrotMemoryWindow.SetListsDirty(true);
            };
        }

        activeList.Dirty = false;
    }

    /// <summary>
    /// Helper method to send a refresh for a given round and set some UI element text to the relevant round number
    /// </summary>
    /// <param name="requestedRoundId">The round ID to change to. If set to null, will request the current round.</param>
    /// <param name="allListsDirty">Whether to set all lists to dirty. If set to false, will only set the current
    /// list to dirty.</param>
    private void ChangeRound(int? requestedRoundId, bool allListsDirty)
    {
        if (allListsDirty)
        {
            ParrotMemoryWindow.SetListsDirty();
        }
        else
        {
            ParrotMemoryWindow.SetActiveListDirty();
        }

        SendRefresh(requestedRoundId);

        var newRoundId = requestedRoundId ?? _currentRound;

        ParrotMemoryWindow.SetRound(newRoundId);
    }

    /// <summary>
    /// Sends a refresh message for the active list. Used whenever a tab is switched, refresh is pressed or some other
    /// reason to reload the memories
    /// </summary>
    /// <param name="roundId">ID of the round to request memories for. If set to null will request current round</param>
    private void SendRefresh(int? roundId)
    {
        // never update on fresh lists
        if (ParrotMemoryWindow.GetActiveList() is not { Dirty: true } activeList)
            return;

        // get rid of old memories here so that the UI appears more responsive. UI design is my passion
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
    /// <param name="memoryId">Player message ID of the memory</param>
    /// <param name="block">True to block, false to unblock</param>
    private void SetMemoryBlocked(int memoryId, bool block)
    {
        SendMessage(new SetParrotMemoryBlockedMsg(memoryId, block));
    }
}
