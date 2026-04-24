using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Trinary.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Localizations;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Atmos.UI;

/// <summary>
/// Initializes a <see cref="GasMixerWindow"/> and updates it when new server messages are received.
/// </summary>
[UsedImplicitly]
public sealed class GasMixerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private const float MaxPressure = Atmospherics.MaxOutputPressure;

    [ViewVariables]
    private GasMixerWindow? _window;

    public GasMixerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GasMixerWindow>();

        _window.ToggleStatusButtonPressed += OnToggleStatusButtonPressed;
        _window.MixerOutputPressureChanged += OnMixerOutputPressurePressed;
        _window.MixerNodePercentageChanged += OnMixerSetPercentagePressed;

        Update();
    }

    private void OnToggleStatusButtonPressed(bool status)
    {
        SendPredictedMessage(new GasMixerToggleStatusMessage(status));
    }

    private void OnMixerOutputPressurePressed(string value)
    {
        var pressure = UserInputParser.TryFloat(value, out var parsed) ? parsed : 0f;
        if (pressure > MaxPressure)
            pressure = MaxPressure;

        SendPredictedMessage(new GasMixerChangeOutputPressureMessage(pressure));
    }

    private void OnMixerSetPercentagePressed(string value)
    {
        // We don't need to send both nodes because it's just 100.0f - node
        var node = UserInputParser.TryFloat(value, out var parsed) ? parsed : 1.0f;

        node = Math.Clamp(node, 0f, 100.0f);

        if (_window is not null)
            node = _window.NodeOneLastEdited ? node : 100.0f - node;

        SendPredictedMessage(new GasMixerChangeNodePercentageMessage(node));
    }

    public override void Update()
    {
        base.Update();

        if (_window == null || !EntMan.TryGetComponent(Owner, out GasMixerComponent? mixer))
            return;

        _window.Title = Identity.Name(Owner, EntMan);
        _window.SetMixerStatus(mixer.Enabled);
        _window.SetOutputPressure(mixer.TargetPressure);
        _window.SetNodePercentages(mixer.InletOneConcentration);
    }
}
