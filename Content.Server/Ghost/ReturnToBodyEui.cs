using Content.Server.EUI;
using Content.Server.Mind;
using Content.Shared.Eui;
using Content.Shared.Ghost;

namespace Content.Server.Ghost;

public sealed class ReturnToBodyEui : BaseEui
{
    private readonly MindSystem _mindSystem;

    private readonly MindComponent _mind;

    public ReturnToBodyEui(MindComponent mind, MindSystem mindSystem)
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

        _mindSystem.UnVisit(_mind.Session);

        Close();
    }
}
