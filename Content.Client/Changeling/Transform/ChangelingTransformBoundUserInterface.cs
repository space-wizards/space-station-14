using Content.Client.Store.Ui;
using Content.Shared.Changeling.Devour;
using Content.Shared.Changeling.Transform;
using Robust.Client.UserInterface;

namespace Content.Client.Changeling.Transform;

public sealed class ChangelingTransformBoundUserInterface : BoundUserInterface
{
    private ChangelingTransformMenu? _menu;
    public ChangelingTransformBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<ChangelingTransformMenu>();
        _menu.Owner(Owner);
        // _menu.OnTransformMenuClicked += SendChangelingTransformRadialMessage;
    }

    public void SendChangelingTransformRadialMessage(ChangelingTransformRadialMessage message)
    {
        SendMessage(message);
    }
}
