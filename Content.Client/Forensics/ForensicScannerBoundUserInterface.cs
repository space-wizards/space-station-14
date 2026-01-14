using Robust.Shared.Timing;
using Content.Shared.Forensics;
using Content.Shared.Forensics.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Forensics;

[UsedImplicitly]
public sealed class ForensicScannerBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    [ViewVariables]
    private ForensicScannerMenu? _window;

    [ViewVariables]
    private TimeSpan _printCooldown;

    public ForensicScannerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<ForensicScannerMenu>();
        _window.Print.OnPressed += _ => Print();
        _window.Clear.OnPressed += _ => Clear();
    }

    private void Print()
    {
        SendPredictedMessage(new ForensicScannerPrintMessage());

        if (_window != null)
            _window.UpdatePrinterState(true);

        // This UI does not require pinpoint accuracy as to when the Print
        // button is available again, so spawning client-side timers is
        // fine. The server will make sure the cooldown is honored.
        Timer.Spawn(_printCooldown, () =>
        {
            if (_window != null)
                _window.UpdatePrinterState(false);
        });
    }

    private void Clear()
    {
        SendPredictedMessage(new ForensicScannerClearMessage());
    }

    public override void Update()
    {
        if (_window == null)
            return;

        if (!EntMan.TryGetComponent(Owner, out ForensicScannerComponent? scanner))
            return;

        _printCooldown = scanner.PrintCooldown;

        // TODO: Fix this
        if (scanner.PrintReadyAt > _gameTiming.CurTime)
        {
            Timer.Spawn(scanner.PrintReadyAt - _gameTiming.CurTime, () =>
            {
                if (_window != null)
                    _window.UpdatePrinterState(false);
            });
        }

        _window.Update(scanner);
    }
}
