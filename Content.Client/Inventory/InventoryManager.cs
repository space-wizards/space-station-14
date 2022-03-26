using Content.Client.Hands;
using Content.Client.Items.Managers;
using Content.Client.UserInterface;
using Content.Client.UserInterface.Controls;
using Content.Shared.Inventory;
using OpenToolkit.Graphics.OpenGL;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Serilog;

namespace Content.Client.Inventory;

public sealed class InventoryUIController : UIController
{
    [Dependency] private EntityManager _entityManager = default!;
    [UISystemDependency] private InventorySystem? _inventorySystem = null;
    private PlayerInventoryData _playerData = new PlayerInventoryData();
    private Dictionary<string, ItemSlotUIContainer> _slotGroups = new();

    public InventoryUIController()
    {
        IoCManager.InjectDependencies(this);
        Test();
    }


    public void Test()
    {
        if (_inventorySystem == null)
        {
            Logger.Debug("Null RIP");
            return;
        }
        Logger.Debug("FOund");
    }


    public readonly struct PlayerInventoryData
    {
        public readonly EntityUid? PlayerEntity;
        public readonly HandsComponent? HandsComponent;
        public readonly  ClientInventoryComponent? InventoryComponent;

        //This provides all the required nullchecking for properties, you can ignore any nullable errors if you use this
        public bool IsValid => (PlayerEntity != null && HandsComponent != null && InventoryComponent != null);
        public PlayerInventoryData(EntityUid? playerEntity, HandsComponent? handsComponent, ClientInventoryComponent? inventoryComponent)
        {
            PlayerEntity = playerEntity;
            HandsComponent = handsComponent;
            InventoryComponent = inventoryComponent;
        }

        public PlayerInventoryData()
        {
            PlayerEntity = null;
            HandsComponent = null;
            InventoryComponent = null;
        }
        public PlayerInventoryData(EntityUid? playerEntity, IEntityManager entityManager)
        {
            if (playerEntity == null)
            {
                PlayerEntity = null;
                HandsComponent = null;
                InventoryComponent = null;
                return;
            }
            PlayerEntity = playerEntity;
            entityManager.TryGetComponent<HandsComponent>(playerEntity, out HandsComponent);
            entityManager.TryGetComponent<ClientInventoryComponent>(playerEntity, out InventoryComponent);
        }
    }
}
