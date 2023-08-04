using Content.Server.EUI;
using Content.Server.Players;
using Content.Shared.Eui;
using Content.Shared.Ghost;

namespace Content.Server.Ghost;

public sealed class ReturnToBodyEui : BaseEui
{
    private readonly Mind.Mind _mind;

    public ReturnToBodyEui(Mind.Mind mind)
    {
        _mind = mind;
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

        if (_mind.TryGetSession(out var session))
            session.ContentData()!.Mind?.UnVisit();
        Close();
    }
}
