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
    private bool _handled; // DS14

    public BecomeRevEui(EntityUid headRevUid, EntityUid targetUid, RevolutionaryRuleSystem revolutionaryRuleSystem)
    {
        _headRevUid = headRevUid;
        _targetUid = targetUid;
        _revolutionaryRuleSystem = revolutionaryRuleSystem;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        // DS14-start
        if (_handled || msg is not BecomeRevChoiceMessage choice)
            return;

        _handled = true;
        if (choice.Button == BecomeRevUiButton.Deny)
        // DS14-end
        {
            Close();
            return;
        }

        _revolutionaryRuleSystem.Convert(_headRevUid, _targetUid);
        Close();
    }
}
