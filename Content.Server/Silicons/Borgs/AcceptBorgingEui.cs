using Content.Server.Chat.Systems;
using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.Mind;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Server.Silicons.Borgs;

public sealed class AcceptBorgingEui : BaseEui
{
    [Dependency] private readonly BorgSystem _borgSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;

    private readonly EntityUid _brain;
    private readonly Entity<MMIComponent> _mmi;
    private readonly Entity<MindComponent> _mind;

    public AcceptBorgingEui(
        EntityUid brain,
        Entity<MMIComponent> mmi,
        Entity<MindComponent> mind,
        IDependencyCollection dependencies)
    {
        dependencies.InjectDependencies(this);
        _brain = brain;
        _mmi = mmi;
        _mind = mind;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        // brain was removed from the MMI.
        if (_mmi.Comp.BrainSlot.Item != _brain)
        {
            Close();
            return;
        }

        if (msg is not AcceptBorgingEuiMessage choice ||
            !choice.Accepted)
        {
            _chatSystem.TrySendInGameICMessage(
                _mmi,
                Loc.GetString("borg-player-denied-borging"),
                InGameICChatType.Speak,
                true);
            Close();
            return;
        }

        _borgSystem.DirectTransferToMMI(_mmi, _mind);
    }
}
