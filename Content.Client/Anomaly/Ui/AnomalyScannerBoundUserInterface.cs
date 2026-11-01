using Content.Shared.Anomaly;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Anomaly.Ui;

/// <summary>
/// A BUI for the anomaly scanner, wraps an <see cref="AnomalyScannerMenu"/>.
/// </summary>
/// <seealso cref="AnomalyScannerComponent"/>
[UsedImplicitly]
public sealed class AnomalyScannerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private AnomalyScannerMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<AnomalyScannerMenu>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not AnomalyScannerUserInterfaceState msg)
            return;

        if (_menu == null)
            return;

        _menu.LastMessage = msg.Message;
        _menu.NextPulseTime = msg.NextPulseTime;
        _menu.UpdateMenu();
    }
}
