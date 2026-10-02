using Content.Shared.Body.Events;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Body;

/// <summary>
/// BUI for the VoiceSexModification menu.
/// </summary>
[UsedImplicitly]
public sealed class VoiceSexModificationControllerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private VoiceSexModificationControllerMenu? _menu;

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
    }

    private void OnConfirmChange(ProtoId<EmoteSoundsPrototype>? voice, Sex sex)
    {
        SendPredictedMessage(new VoiceSexModificationMessage(voice, sex));
    }
}
