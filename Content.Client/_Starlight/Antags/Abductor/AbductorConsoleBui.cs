using Content.Shared.Starlight.Antags.Abductor;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;
using static Content.Shared.Pinpointer.SharedNavMapSystem;

namespace Content.Client._Starlight.Antags.Abductor;

[UsedImplicitly]
public sealed class AbductorConsoleBui : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entities = default!;

    [ViewVariables]
    private AbductorConsoleWindow? _window;
    public AbductorConsoleBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }
    protected override void Open() => UpdateState(State);
    protected override void UpdateState(BoundUserInterfaceState? state)
    {
        if (state is AbductorConsoleBuiState s)
            Update(s);
    }

    private void Update(AbductorConsoleBuiState state)
    {
        TryInitWindow();

        View(ViewType.Teleport);

        RefreshUI();

        if (!_window!.IsOpen)
            _window.OpenCentered();
    }

    private void TryInitWindow()
    {
        if (_window != null) return;
        _window = new AbductorConsoleWindow();
        _window.OnClose += Close;
        _window.Title = "console";

        _window.TeleportTabButton.OnPressed += _ => View(ViewType.Teleport);

        _window.ExperimentTabButton.OnPressed += _ => View(ViewType.Experiment);
    }

    private void RefreshUI()
    {
        if (_window == null || State is not AbductorConsoleBuiState state)
            return;

        // teleportTab
        _window.TargetLabel.Children.Clear();

        var padMsg = new FormattedMessage();
        padMsg.AddMarkupOrThrow(state.AlienPadFound ? "pad: [color=green]connected[/color]" : "pad: [color=red]not found[/color]");
        _window.PadLabel.SetMessage(padMsg);

        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(state.Target == null ? "target: [color=red]NONE[/color]" : $"target: [color=green]{state.TargetName}[/color]");
        _window.TeleportButton.Disabled = state.Target == null || !state.AlienPadFound;
        _window.TeleportButton.OnPressed += _ =>
        {
            SendMessage(new AbductorAttractBuiMsg());
            Close();
        };
        _window.TargetLabel.SetMessage(msg, new Type[1] { typeof(ColorTag) });

        // experiment tab

        var experimentatorMsg = new FormattedMessage();
        experimentatorMsg.AddMarkupOrThrow(state.AlienPadFound ? "experimentator: [color=green]connected[/color]" : "experimentator: [color=red]not found[/color]");
        _window.ExperimentatorLabel.SetMessage(experimentatorMsg);

        var victimMsg = new FormattedMessage();
        victimMsg.AddMarkupOrThrow(state.VictimName == null ? "victim: [color=red]NONE[/color]" : $"victim: [color=green]{state.VictimName}[/color]");
        _window.VictimLabel.SetMessage(victimMsg);

        _window.CompleteExperimentButton.Disabled = state.VictimName == null;
        _window.CompleteExperimentButton.OnPressed += _ =>
        {
            SendMessage(new AbductorCompleteExperimentBuiMsg());
            Close();
        };
    }

    private void View(ViewType type)
    {
        if (_window == null)
            return;

        _window.TeleportTabButton.Parent!.Margin = new Thickness(0, 0, 0, 10);

        _window.TeleportTabButton.Disabled = type == ViewType.Teleport;
        _window.ExperimentTabButton.Disabled = type == ViewType.Experiment;
        _window.TeleportTab.Visible = type == ViewType.Teleport;
        _window.ExperimentTab.Visible = type == ViewType.Experiment;
    }

    private enum ViewType
    {
        Teleport,
        Experiment
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Dispose();
    }
}
