using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Clothing;
using Content.Client.Items.UI;
using Content.Shared.CharacterAppearance;
using Content.Shared.Inventory;
using Content.Shared.Movement.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using static Content.Shared.Inventory.SharedInventoryComponent.ClientInventoryMessage;

namespace Content.Client.Inventory
{
    /// <summary>
    /// A character UI which shows items the user has equipped within his inventory
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(InventoryComponent))]
    [Friend(typeof(ClientInventorySystem))]
    public class ClientInventoryComponent : InventoryComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        public Control BottomLeftButtons = default!;
        public Control BottomRightButtons = default!;
        public Control TopQuickButtons = default!;

        public SS14Window InventoryWindow = default!;

        public readonly Dictionary<string, List<ItemSlotButton>> SlotButtons = new();

        [ViewVariables]
        [DataField("speciesId")] public string? SpeciesId { get; set; }

        public void SendEquipMessage(Slots slot)
        {
            var equipMessage = new ClientInventoryMessage(slot, ClientInventoryUpdate.Equip);
#pragma warning disable 618
            SendNetworkMessage(equipMessage);
#pragma warning restore 618
        }

        public void SendUseMessage(Slots slot)
        {
            var equipmessage = new ClientInventoryMessage(slot, ClientInventoryUpdate.Use);
#pragma warning disable 618
            SendNetworkMessage(equipmessage);
#pragma warning restore 618
        }

        public void SendOpenStorageUIMessage(Slots slot)
        {
#pragma warning disable 618
            SendNetworkMessage(new OpenSlotStorageUIMessage(slot));
#pragma warning restore 618
        }
    }
}
