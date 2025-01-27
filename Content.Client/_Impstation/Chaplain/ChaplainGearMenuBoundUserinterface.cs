using Content.Shared._Impstation.Chaplain;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._Impstation.Chaplain;

[UsedImplicitly]
public sealed class ChaplainGearMenuBoundUserInterface : BoundUserInterface
{
    private ChaplainGearMenu? _window;

    public ChaplainGearMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ChaplainGearMenu>();
        _window.OnApprove += SendApprove;
        _window.OnSetChange += SendChangeSelected;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ChaplainGearMenuBoundUserInterfaceState current)
            return;

        _window?.UpdateState(current);
    }

    public void SendChangeSelected(int setNumber)
    {
        SendMessage(new ChaplainGearChangeSetMessage(setNumber));
    }

    public void SendApprove()
    {
        SendMessage(new ChaplainGearMenuApproveMessage());
    }
}
