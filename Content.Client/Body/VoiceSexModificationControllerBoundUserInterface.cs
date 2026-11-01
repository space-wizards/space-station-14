using Content.Shared.Body.Events;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Body;

/// <summary>
/// BUI for the VoiceSexModificationController menu.
/// </summary>
[UsedImplicitly]
public sealed class VoiceSexModificationControllerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private VoiceSexModificationControllerMenu? _menu;

    [ViewVariables]
    private Sex _currentSex;

    [ViewVariables]
    private ProtoId<EmoteSoundsPrototype>? _currentVoice;

    /// <inheritdoc/>
    public VoiceSexModificationControllerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    /// <inheritdoc/>
    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<VoiceSexModificationControllerMenu>();
        _menu.OnConfirmChange += OnConfirmChange;
    }

    /// <inheritdoc/>
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not VoiceSexModificationControllerBuiState bState)
            return;

        _menu?.UpdateState(bState);

        _currentVoice = bState.CurrentVoice;
        _currentSex = bState.CurrentSex;
    }

    private void OnConfirmChange(ProtoId<EmoteSoundsPrototype>? voice, Sex sex)
    {
        if (_currentVoice == voice && _currentSex == sex)
            return;

        SendPredictedMessage(new VoiceSexModificationMessage(voice, sex));
    }
}
