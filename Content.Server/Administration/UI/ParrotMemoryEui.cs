using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.Administration.ParrotMemories;
using Content.Shared.Eui;

namespace Content.Server.Administration.UI;

public sealed class ParrotMemoryEui : BaseEui
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;

    private readonly List<ExtendedParrotMemory> _parrotMemories = [];
    private readonly int _currentRound;
    private int _requestedRound;
    private bool _showBlocked;
    private string _textFilter = string.Empty;

    public ParrotMemoryEui()
    {
        IoCManager.InjectDependencies(this);

        _currentRound = _entityManager.System<GameTicker>().RoundId;
    }


    public override EuiStateBase GetNewState()
    {
        return new ParrotMemoryEuiState(_parrotMemories, _currentRound, _requestedRound);
    }

    public override async void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (!_adminManager.HasAdminFlag(Player, AdminFlags.Moderator))
            return;

        switch (msg)
        {
            case ParrotMemoryRefreshMsg refreshMsg:
                _showBlocked = refreshMsg.ShowBlocked;
                _textFilter = refreshMsg.TextFilter;

                // requestedround can be null if the current round is requested
                // this will usually be when first opening or returning to current round
                _requestedRound = refreshMsg.RequestedRoundId ?? _currentRound;

                RefreshParrotMemories(_requestedRound);

                break;
            case SetParrotMemoryBlockedMsg blockChangeMsg:
                SetParrotMemoryBlock(blockChangeMsg.MemoryId, blockChangeMsg.Block);
                break;
        }
    }

    private async void SetParrotMemoryBlock(int memoryId, bool block)
    {
        await _db.SetParrotMemoryBlock(memoryId, block);
    }

    private async void RefreshParrotMemories(int roundId)
    {
        string? textFilter = null;

        if (_textFilter != string.Empty)
            textFilter = _textFilter;

        var memories = _db.GetParrotMemories(_showBlocked, roundId, textFilter);

        _parrotMemories.Clear();

        await foreach (var memory in memories)
        {
            _parrotMemories.Add(memory);
        }

        StateDirty();
    }
}
