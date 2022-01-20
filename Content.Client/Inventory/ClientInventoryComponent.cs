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
    public class ClientInventoryComponent : InventoryComponent
    {
        public Control BottomLeftButtons = default!;
        public Control BottomRightButtons = default!;
        public Control TopQuickButtons = default!;

        public SS14Window InventoryWindow = default!;

        public readonly Dictionary<string, List<ItemSlotButton>> SlotButtons = new();

        [ViewVariables]
        [DataField("speciesId")] public string? SpeciesId { get; set; }

        /// <summary>
        ///     Data about the current layers that have been added to the players sprite due to the items in each equipment slot.
        /// </summary>
        [ViewVariables]
        public readonly Dictionary<string, HashSet<string>> VisualLayerKeys = new();

        public bool AttachedToGameHud;
    }
}
