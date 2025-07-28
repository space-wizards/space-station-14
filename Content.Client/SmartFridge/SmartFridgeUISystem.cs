using Content.Shared.SmartFridge;
using Robust.Shared.Analyzers;

namespace Content.Client.SmartFridge;

public sealed class SmartFridgeUISystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SmartFridgeComponent, AfterAutoHandleStateEvent>(OnSmartFridgeAfterState);
    }

    private void OnSmartFridgeAfterState(Entity<SmartFridgeComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!_uiSystem.TryGetOpenUi<SmartFridgeBoundUserInterface>(ent.Owner, SmartFridgeUiKey.Key, out var bui))
            return;

        bui.Refresh();
    }
}
