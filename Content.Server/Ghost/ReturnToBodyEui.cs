using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Ghost;

public sealed class ReturnToBodyEui : BaseEui
{
    private readonly SharedMindSystem _mindSystem;
    private readonly ISharedPlayerManager _player;
    private readonly NetUserId? _userId;

    public ReturnToBodyEui(MindComponent mind, SharedMindSystem mindSystem, ISharedPlayerManager player)
    {
        _mindSystem = mindSystem;
        _player = player;
        _userId = mind.UserId;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not ReturnToBodyMessage choice ||
            !choice.Accepted)
        {
            Close();
            return;
        }

        if (_userId is { } userId && _player.TryGetSessionById(userId, out var session))
            _mindSystem.UnVisit(session);

        Close();
    }
}
