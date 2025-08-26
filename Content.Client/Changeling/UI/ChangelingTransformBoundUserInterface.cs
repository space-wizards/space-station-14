using Content.Shared.Changeling.Systems;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Changeling.UI;

[UsedImplicitly]
public sealed partial class ChangelingTransformBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private ChangelingTransformMenu? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ChangelingTransformMenu>();

        _window.OnIdentitySelect += SendIdentitySelect;

        _window.Update(Owner);
    }

    public override void Update()
    {
        if (_window == null)
            return;

        _window.Update(Owner);
    }

    public void SendIdentitySelect(NetEntity identityId)
    {
        SendPredictedMessage(new ChangelingTransformIdentitySelectMessage(identityId));
    }
}
