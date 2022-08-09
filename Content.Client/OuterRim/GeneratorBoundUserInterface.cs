using Content.Shared.OuterRim.Generator;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.OuterRim;

[UsedImplicitly]
public sealed class GeneratorBoundUserInterface : BoundUserInterface
{
    private GeneratorWindow? _window;

    public GeneratorBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = new GeneratorWindow(this, Owner.Owner);

        _window.OpenCenteredLeft();
        _window.OnClose += Close;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        _window?.Update((GeneratorComponentBuiState)state);
    }

    protected override void Dispose(bool disposing)
    {
       _window?.Dispose();
    }

    public void SetTargetPower(int target)
    {
        SendMessage(new SetTargetPowerMessage(target));
    }
}
