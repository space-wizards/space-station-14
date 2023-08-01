using Content.Server.EUI;
using Content.Server.Mind;
using Content.Server.Players;
using Content.Shared.Eui;
using Content.Shared.Ghost;

namespace Content.Server.Ghost;

public sealed class ReturnToBodyEui : BaseEui
{
    private readonly MindSystem _mindSystem;

    private readonly Mind.Mind _mind;

    public ReturnToBodyEui(Mind.Mind mind, MindSystem mindSystem)
    {
        _mind = mind;
        _mindSystem = mindSystem;
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

        if (_mindSystem.TryGetSession(_mind, out var session))
            _mindSystem.UnVisit(session.ContentData()!.Mind);
        Close();
    }
}
