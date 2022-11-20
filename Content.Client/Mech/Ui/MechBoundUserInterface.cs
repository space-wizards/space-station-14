using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Mech.Ui;

[UsedImplicitly]
public sealed class MechBoundUserInterface : BoundUserInterface
{
    public MechBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    protected override void Open()
    {
        base.Open();


    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

    }
}

