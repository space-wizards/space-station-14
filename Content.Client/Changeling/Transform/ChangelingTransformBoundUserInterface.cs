using Content.Shared.Changeling.Transform;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Changeling.Transform;

[UsedImplicitly]
public sealed partial class ChangelingTransformBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private ChangelingTransformMenu? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ChangelingTransformMenu>();

        _window.OnIdentitySelect += SendIdentitySelect;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ChangelingTransformBoundUserInterfaceState current)
            return;

        _window?.UpdateState(current);
    }

    public void SendIdentitySelect(NetEntity identityId)
    {
        SendPredictedMessage(new ChangelingTransformIdentitySelectMessage(identityId));
    }
}
