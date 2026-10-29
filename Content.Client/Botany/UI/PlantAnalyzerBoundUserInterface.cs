using Content.Shared.Botany;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Botany.UI;

/// <summary>
/// Displays the plant analyzer user interface for an entity.
/// </summary>
[UsedImplicitly]
public sealed class PlantAnalyzerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private PlantAnalyzerWindow? _window;
    private BotanyAnalyzerState? _state;

    /// <inheritdoc />
    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<PlantAnalyzerWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        Update();
    }

    /// <inheritdoc />
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not BotanyAnalyzerState botany)
            return;

        _state = botany;
    }

    /// <inheritdoc />
    public override void Update()
    {
        base.Update();

        if (_state is not { } state)
            return;

        _window?.Update(state);
    }
}
