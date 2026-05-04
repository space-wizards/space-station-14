using Content.Client.UserInterface.Controls;
using Content.Shared._Offbrand.Analyzers;
using Robust.Client.UserInterface;

namespace Content.Client._Offbrand.Analyzer;

public sealed class VitalsAnalyzerBoundUserInterface : BoundUserInterface
{
    private VitalsAnalyzerControl _control = new();
    private FancyWindow? _window;

    public VitalsAnalyzerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<FancyWindow>();
        _window.AddChild(_control);
    }

    public override void Update()
    {
        base.Update();

        if (EntMan.TryGetComponent<VitalsAnalyzerComponent>(Owner, out var vitalsAnalyzer) && vitalsAnalyzer.Data is { } data)
        {
            _control.Update(data);
        }
    }
}
