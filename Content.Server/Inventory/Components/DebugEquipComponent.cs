using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Inventory.Components
{
    /// <summary>
    /// Pops up a message when equipped / unequipped (including hands).
    /// For debugging purposes.
    /// </summary>
    [RegisterComponent]
    public class DebugEquipComponent : Component, IEquipped, IEquippedHand, IUnequipped, IUnequippedHand
    {
        public override string Name => "DebugEquip";

        void IEquipped.Equipped(EquippedEventArgs eventArgs)
        {
            eventArgs.User.PopupMessage("equipped " + IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Owner.Uid).EntityName);
        }

        void IEquippedHand.EquippedHand(EquippedHandEventArgs eventArgs)
        {
            eventArgs.User.PopupMessage("equipped hand " + IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Owner.Uid).EntityName);
        }

        void IUnequipped.Unequipped(UnequippedEventArgs eventArgs)
        {
            eventArgs.User.PopupMessage("unequipped " + IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Owner.Uid).EntityName);
        }

        void IUnequippedHand.UnequippedHand(UnequippedHandEventArgs eventArgs)
        {
            eventArgs.User.PopupMessage("unequipped hand" + IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Owner.Uid).EntityName);
        }
    }
}
