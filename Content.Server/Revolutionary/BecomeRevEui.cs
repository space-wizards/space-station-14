using Content.Server.EUI;
using Content.Server.GameTicking.Rules;
using Content.Shared.Eui;
using Content.Shared.Revolutionary;

namespace Content.Server.Revolutionary;

public sealed class BecomeRevEui : BaseEui
{
    private readonly EntityUid _headRevUid;
    private readonly EntityUid _targetUid;
    private readonly RevolutionaryRuleSystem _revolutionaryRuleSystem;

    public BecomeRevEui(EntityUid headRevUid, EntityUid targetUid, RevolutionaryRuleSystem revolutionaryRuleSystem)
    {
        _headRevUid = headRevUid;
        _targetUid = targetUid;
        _revolutionaryRuleSystem = revolutionaryRuleSystem;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not BecomeRevChoiceMessage choice ||
            choice.Button == BecomeRevUiButton.Deny)
        {
            Close();
            return;
        }

        _revolutionaryRuleSystem.Convert(_headRevUid, _targetUid);
        Close();
    }
}
