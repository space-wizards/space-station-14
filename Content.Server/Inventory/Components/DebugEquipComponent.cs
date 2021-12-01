using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;

namespace Content.Server.Inventory.Components
{
    /// <summary>
    /// Pops up a message when equipped / unequipped (NOT including hands).
    /// For debugging purposes.
    /// </summary>
    [RegisterComponent]
    public class DebugEquipComponent : Component, IEquipped, IUnequipped
    {
        public override string Name => "DebugEquip";

        void IEquipped.Equipped(EquippedEventArgs eventArgs)
        {
            eventArgs.User.PopupMessage("equipped " + Owner.Name);
        }

        /* If someone needs this, they can go ahead and add a system for it that uses EquippedHandEvent

        void IEquippedHand.EquippedHand(EquippedHandEventArgs eventArgs)
        {
            eventArgs.User.PopupMessage("equipped hand " + Owner.Name);
        }

        void IUnequippedHand.UnequippedHand(UnequippedHandEventArgs eventArgs)
        {
            eventArgs.User.PopupMessage("unequipped hand" + Owner.Name);
        }
        */

        void IUnequipped.Unequipped(UnequippedEventArgs eventArgs)
        {
            eventArgs.User.PopupMessage("unequipped " + Owner.Name);
        }
    }
}
