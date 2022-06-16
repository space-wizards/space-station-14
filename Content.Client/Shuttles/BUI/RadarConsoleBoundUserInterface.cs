using Content.Client.Shuttles.UI;
using Content.Shared.Shuttles.BUIStates;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Shuttles.BUI;

[UsedImplicitly]
public sealed class RadarConsoleBoundUserInterface : BoundUserInterface
{
    private RadarConsoleWindow? _window;

    public RadarConsoleBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey) {}

    protected override void Open()
    {
        base.Open();
        _window = new RadarConsoleWindow();
        _window?.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not RadarConsoleBoundInterfaceState cState) return;

        _window?.UpdateState(cState);
    }
}
