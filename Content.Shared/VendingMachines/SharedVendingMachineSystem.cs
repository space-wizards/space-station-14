using Robust.Shared.Prototypes;
using static Content.Shared.VendingMachines.SharedVendingMachineComponent;

namespace Content.Shared.VendingMachines;

public abstract class SharedVendingMachineSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SharedVendingMachineComponent, ComponentInit>(OnComponentInit);;
    }

    protected virtual void OnComponentInit(EntityUid uid, SharedVendingMachineComponent component, ComponentInit args)
    {
        if (!_prototypeManager.TryIndex(component.PackPrototypeId, out VendingMachineInventoryPrototype? packPrototype))
            return;

        MetaData(uid).EntityName = packPrototype.Name;
        component.AnimationDuration = TimeSpan.FromSeconds(packPrototype.AnimationDuration);

        if (TryComp(component.Owner, out AppearanceComponent? appearance))
            appearance.SetData(VendingMachineVisuals.Inventory, component.PackPrototypeId);

        AddInventoryFromPrototype(uid, packPrototype.StartingInventory, InventoryType.Regular, component);
        AddInventoryFromPrototype(uid, packPrototype.EmaggedInventory, InventoryType.Emagged, component);
        AddInventoryFromPrototype(uid, packPrototype.ContrabandInventory, InventoryType.Contraband, component);
    }

    private void AddInventoryFromPrototype(EntityUid uid, Dictionary<string, uint>? entries,
        InventoryType type,
        SharedVendingMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component) || entries == null)
        {
            return;
        }

        var inventory = new List<VendingMachineInventoryEntry>();

        foreach (var (id, amount) in entries)
        {
            if (_prototypeManager.HasIndex<EntityPrototype>(id))
            {
                inventory.Add(new VendingMachineInventoryEntry(type, id, amount));
            }
        }

        switch (type)
        {
            case InventoryType.Regular:
                component.Inventory.AddRange(inventory);
                break;
            case InventoryType.Emagged:
                component.EmaggedInventory.AddRange(inventory);
                break;
            case InventoryType.Contraband:
                component.ContrabandInventory.AddRange(inventory);
                break;
        }
    }
}

