using Content.Shared.Hands.Components;

namespace Content.Shared.Inventory;

[InjectDependencies]
public partial class InventorySystem
{

    public override void Initialize()
    {
        base.Initialize();
        InitializeEquip();
        InitializeRelay();
        InitializeSlots();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        ShutdownSlots();
    }
}
