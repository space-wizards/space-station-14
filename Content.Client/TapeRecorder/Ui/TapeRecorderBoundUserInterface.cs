using Content.Shared.TapeRecorder.Components;
using Content.Shared.TapeRecorder.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.TapeRecorder.Ui;

public sealed class TapeRecorderBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    [ViewVariables]
    private TapeRecorderWindow? _window;

    [ViewVariables]
    private TimeSpan _printCooldown;

    protected override void Open()
    {
        base.Open();

        _window = new(_entMan, Owner);
        _window.OnClose += Close;
        _window.OnModeChanged += ChangeMode;
        _window.OnPrintTranscript += PrintTranscript;
        _window.OpenCentered();
    }

    private void ChangeMode(TapeRecorderMode mode)
    {
        SendMessage(new ChangeModeTapeRecorderMessage(mode));
    }

    private void PrintTranscript()
    {
        SendMessage(new PrintTapeRecorderMessage());

        _window?.UpdatePrint(true);

        Timer.Spawn(_printCooldown, () =>
        {
            _window?.UpdatePrint(false);
        });
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not TapeRecorderState cast)
            return;

        _printCooldown = cast.PrintCooldown;

        _window?.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _window?.Dispose();
    }
}

