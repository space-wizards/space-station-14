using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.Administration.ParrotMessages;
using Content.Shared.Eui;

namespace Content.Server.Administration.UI;

public sealed class ParrotMessagesEui : BaseEui
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private readonly List<ExtendedPlayerMessage> _parrotMessages = [];
    private readonly int _currentRoundId;
    private bool _currentRoundOnly;
    private bool _showBlocked;
    private string _textFilter = string.Empty;

    public ParrotMessagesEui()
    {
        IoCManager.InjectDependencies(this);

        _currentRoundId = _entityManager.System<GameTicker>().RoundId;
    }


    public override EuiStateBase GetNewState()
    {
        return new ParrotMessagesEuiState(_parrotMessages);
    }

    public override async void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (!_adminManager.HasAdminFlag(Player, AdminFlags.Moderator))
            return;

        switch (msg)
        {
            case ParrotMessageRefreshMsg refreshMsg:
                _currentRoundOnly = refreshMsg.CurrentRoundOnly;
                _showBlocked = refreshMsg.ShowBlocked;
                RefreshParrotMessages();

                break;
            case ParrotMessageBlockChangeMsg blockChangeMsg:
                SetParrotMessageBlock(blockChangeMsg.MessageId, blockChangeMsg.Block);
                break;

            case ParrotMessageFilterChangeMsg filterChangeMsg:
                _textFilter = filterChangeMsg.FilterString;
                RefreshParrotMessages();
                break;
        }
    }

    private async void SetParrotMessageBlock(int messageId, bool block)
    {
        await Task.Run(async () => await _db.SetParrotMemoryBlock(messageId, block));
    }

    private async void RefreshParrotMessages()
    {
        int? round = null;

        if (_currentRoundOnly)
            round = _currentRoundId;

        string? textFilter = null;

        if (_textFilter != string.Empty)
            textFilter = _textFilter;

        var messages = _db.GetParrotMemories(_showBlocked, round, textFilter);

        _parrotMessages.Clear();

        await foreach (var message in messages)
        {
            _parrotMessages.Add(message);
        }

        StateDirty();
    }
}
