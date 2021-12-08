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
        [Dependency] private readonly IEntityManager _entMan = default!;

        public override string Name => "DebugEquip";

        void IEquipped.Equipped(EquippedEventArgs eventArgs)
        {
            eventArgs.User.PopupMessage("equipped " + _entMan.GetComponent<MetaDataComponent>(Owner).EntityName);
        }

        void IEquippedHand.EquippedHand(EquippedHandEventArgs eventArgs)
        {
            eventArgs.User.PopupMessage("equipped hand " + _entMan.GetComponent<MetaDataComponent>(Owner).EntityName);
        }

        void IUnequipped.Unequipped(UnequippedEventArgs eventArgs)
        {
            eventArgs.User.PopupMessage("unequipped " + _entMan.GetComponent<MetaDataComponent>(Owner).EntityName);
        }

        void IUnequippedHand.UnequippedHand(UnequippedHandEventArgs eventArgs)
        {
            eventArgs.User.PopupMessage("unequipped hand" + _entMan.GetComponent<MetaDataComponent>(Owner).EntityName);
        }
    }
}
