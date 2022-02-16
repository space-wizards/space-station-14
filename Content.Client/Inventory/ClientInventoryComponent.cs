using System.Collections.Generic;
using Content.Client.Items.UI;
using Content.Shared.Inventory;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Client.Inventory
{
    /// <summary>
    /// A character UI which shows items the user has equipped within his inventory
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(InventoryComponent))]
    [Friend(typeof(ClientInventorySystem))]
    public sealed class ClientInventoryComponent : InventoryComponent
    {
        public Control BottomLeftButtons = default!;
        public Control BottomRightButtons = default!;
        public Control TopQuickButtons = default!;

        public DefaultWindow InventoryWindow = default!;

        public readonly Dictionary<string, List<ItemSlotButton>> SlotButtons = new();

        [ViewVariables]
        [DataField("speciesId")] public string? SpeciesId { get; set; }

        public bool AttachedToGameHud;
    }
}
