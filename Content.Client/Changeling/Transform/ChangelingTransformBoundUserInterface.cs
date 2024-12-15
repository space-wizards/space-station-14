

using Content.Shared.Changeling.Transform;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Changeling.Transform;

[UsedImplicitly]
public sealed class ChangelingTransformBoundUserInterface : BoundUserInterface
{
    private ChangelingTransformMenu? _window;

    public ChangelingTransformBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<ChangelingTransformMenu>();
        _window.OnIdentitySelect += SendIdentitySelect;
    }

    // protected override void Dispose(bool disposing)
    // {
    //     base.Dispose(disposing);
    //     _window?.CleanUp(); // Tell the window to destroy all the Client Nullspace entities it made to allow spriteview
    // }


    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ChangelingTransformBoundUserInterfaceState current)
            return;

        _window?.UpdateState(current);
    }

    public void SendIdentitySelect(NetEntity identityId)
    {
        SendMessage(new ChangelingTransformIdentitySelectMessage(identityId));
    }


}
