// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.CustomizableHumanoidSpawner;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.DeadSpace.CustomizableHumanoidSpawner;

[UsedImplicitly]
public sealed class CustomizableHumanoidSpawnerBUI(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private CustomizableHumanoidSpawnerUI? _ui;

    protected override void Open()
    {
        base.Open();
        _ui = this.CreateWindow<CustomizableHumanoidSpawnerUI>();
        _ui.OnConfirm += Send;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (_ui == null || state is not CustomizableHumanoidSpawnerBuiState msg)
            return;

        _ui.SetData(msg);
    }

    private void Send(
        bool useRandom,
        int characterIndex,
        string customName,
        bool useCustomDescription,
        string customDescription)
    {
        SendMessage(new CustomizableHumanoidSpawnerMessage(
            useRandom,
            characterIndex,
            customName,
            useCustomDescription,
            customDescription));
    }
}
