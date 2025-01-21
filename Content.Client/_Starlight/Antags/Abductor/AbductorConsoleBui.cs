using Content.Shared.Starlight.Antags.Abductor;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;
using static Content.Shared.Pinpointer.SharedNavMapSystem;
using static Robust.Client.UserInterface.Control;

namespace Content.Client._Starlight.Antags.Abductor;

[UsedImplicitly]
public sealed class AbductorConsoleBui : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entities = default!;

    [ViewVariables]
    private AbductorConsoleWindow? _window;
    [ViewVariables]
    private bool armorDisabled = false;
    [ViewVariables]
    private bool armorLocked = false;
    [ViewVariables]
    private string? armorMode;
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
        
        _window.ArmorControlTabButton.OnPressed += _ => View(ViewType.ArmorControl);
        
        _window.CombatModeButton.OnPressed += _ => {
            _window.StealthModeButton.Disabled = false;
            _window.CombatModeButton.Disabled = true;
            SendMessage(new AbductorVestModeChangeBuiMsg()
            {
                Mode = "combat",
            });
        };
        
        _window.StealthModeButton.OnPressed += _ => {
            _window.StealthModeButton.Disabled = true;
            _window.CombatModeButton.Disabled = false;
            SendMessage(new AbductorVestModeChangeBuiMsg()
            {
                Mode = "stealth",
            });
        };
        
        if (armorMode == "combat")
        {
            _window.CombatModeButton.Disabled = true;
            _window.StealthModeButton.Disabled = false;
        }
        else
        {
            _window.CombatModeButton.Disabled = false;
            _window.StealthModeButton.Disabled = true;
        }
        
        _window.LockArmorButton.OnPressed += _ =>
        {
            SendMessage(new AbductorLockBuiMsg());
            
            armorLocked = !armorLocked;
            
            if (!armorLocked)
                _window.LockArmorButton.Text = Loc.GetString("abductors-ui-lock-armor");
            else
                _window.LockArmorButton.Text = Loc.GetString("abductors-ui-unlock-armor");
        };
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
        
        var dispencerMsg = new FormattedMessage();
        dispencerMsg.AddMarkupOrThrow(state.DispencerFound ? "dispencer: [color=green]connected[/color]" : "dispencer: [color=red]not found[/color]");
        _window.DispencerLabel.SetMessage(dispencerMsg);

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
        
        // armor tab     
        armorLocked = state.ArmorLocked;
        
        if (!armorLocked)
            _window.LockArmorButton.Text = Loc.GetString("abductors-ui-lock-armor");
        else
            _window.LockArmorButton.Text = Loc.GetString("abductors-ui-unlock-armor");
        
        armorDisabled = state.ArmorFound;
        
        armorMode = state.CurrentArmorMode;
        
        if (armorMode == "combat")
        {
            _window.CombatModeButton.Disabled = true;
            _window.StealthModeButton.Disabled = false;
        }
        else
        {
            _window.CombatModeButton.Disabled = false;
            _window.StealthModeButton.Disabled = true;
        }
        
        UpdateDisabledPanel(armorDisabled);
    }
    
    private void UpdateDisabledPanel(bool disable)
    {
        if (_window == null)
            return;

        if (disable || !_window.ArmorControlTab.Visible)
        {
            _window.DisabledPanel.Visible = false;
            _window.DisabledPanel.MouseFilter = MouseFilterMode.Ignore;
            return;
        }

        _window.DisabledPanel.Visible = true;
        if (_window.DisabledLabel.GetMessage() is null)
        {
            var text = new FormattedMessage();
            text.AddMarkupOrThrow("[color=red][font size=16]You need to plug in abductor armor![/font][/color]");
            _window.DisabledLabel.SetMessage(text);
        }

        _window.DisabledPanel.MouseFilter = MouseFilterMode.Stop;
    }

    private void View(ViewType type)
    {
        if (_window == null)
            return;

        _window.TeleportTabButton.Parent!.Margin = new Thickness(0, 0, 0, 10);

        _window.TeleportTabButton.Disabled = type == ViewType.Teleport;
        _window.ExperimentTabButton.Disabled = type == ViewType.Experiment;
        _window.ArmorControlTabButton.Disabled = type == ViewType.ArmorControl;
        _window.TeleportTab.Visible = type == ViewType.Teleport;
        _window.ExperimentTab.Visible = type == ViewType.Experiment;
        _window.ArmorControlTab.Visible = type == ViewType.ArmorControl;
        
        UpdateDisabledPanel(armorDisabled);
    }

    private enum ViewType
    {
        Teleport,
        Experiment,
        ArmorControl
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Dispose();
    }
}
