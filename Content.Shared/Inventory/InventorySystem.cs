namespace Content.Shared.Inventory;

public partial class InventorySystem
{

    public override void Initialize()
    {
        base.Initialize();
        InitializeEquip();
        InitializeSlots();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        ShutdownSlots();
    }
}
