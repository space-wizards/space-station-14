using Robust.Client.UserInterface;
using Content.Shared.Arcade;

namespace Content.Client.Arcade.UI;

public sealed class MineGameBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables] private MineGameArcadeWindow? _window;
    private bool _difficultiesLoaded = false;

    public void SendTileAction(MineGameTileAction action)
    {
        SendPredictedMessage(new MineGameTileActionMessage(action));
    }

    public void SetBoardAction(MineGameBoardSettings settings)
    {
        SendPredictedMessage(new MineGameRequestNewBoardMessage(settings));
    }

    /// <inheritdoc/>
    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<MineGameArcadeWindow>();
        _window.OnTileAction += SendTileAction;
        _window.OnBoardSettingAction += SetBoardAction;
        Update();
    }

    /// <inheritdoc/>
    public override void Update()
    {
        base.Update();

        if (_window is null || !EntMan.TryGetComponent(Owner, out MineGameArcadeComponent? component))
            return;

        if (!_difficultiesLoaded)
        {
            _window?.LoadPresetDifficulties(component.BoardPresets);
            _difficultiesLoaded = true;
        }

        _window?.UpdateBoard(component);
    }
}
