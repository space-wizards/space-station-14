using Content.Shared.SmartFridge;

namespace Content.Client.SmartFridge;

public sealed class SmartFridgeSystem : SharedSmartFridgeSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    protected override void UpdateUI(Entity<SmartFridgeComponent> ent)
    {
        base.UpdateUI(ent);

        if (!_uiSystem.TryGetOpenUi<SmartFridgeBoundUserInterface>(ent.Owner, SmartFridgeUiKey.Key, out var bui))
            return;

        bui.Refresh();
    }
}
