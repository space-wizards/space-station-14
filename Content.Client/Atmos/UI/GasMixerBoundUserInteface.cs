using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Trinary.Components;
using Content.Shared.IdentityManagement;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Atmos.UI;

/// <summary>
/// Initializes a <see cref="GasMixerWindow"/> and updates it when new server messages are received.
/// </summary>
[UsedImplicitly]
public sealed class GasMixerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private GasMixerWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GasMixerWindow>();

        _window.ToggleStatusButtonPressed += OnToggleStatusButtonPressed;
        _window.MixerOutputPressureChanged += OnMixerOutputPressurePressed;
        _window.MixerNodePercentageChanged += OnMixerSetPercentagePressed;
        Update();
    }

    private void OnToggleStatusButtonPressed()
    {
        if (_window is null) return;
        SendMessage(new GasMixerToggleStatusMessage(_window.MixerStatus));
    }

    private void OnMixerOutputPressurePressed(float value)
    {
        SendMessage(new GasMixerChangeOutputPressureMessage(value));
    }

    private void OnMixerSetPercentagePressed(float value)
    {
        SendMessage(new GasMixerChangeNodePercentageMessage(value));
    }

    public override void Update()
    {
        if (_window == null)
            return;

        _window.Title = Identity.Name(Owner, EntMan);

        if (!EntMan.TryGetComponent(Owner, out GasMixerComponent? mixer))
            return;

        _window.SetMixerStatus(mixer.Enabled);
        _window.MaxPressure = mixer.MaxTargetPressure;
        _window.SetOutputPressure(mixer.TargetPressure);
        _window.SetNodePercentages(mixer.InletOneConcentration);
    }
}
