using Content.Shared.GameObjects.Components.Items;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Client.Interfaces.GameObjects
{
    // HYPER SIMPLE HANDS API CLIENT SIDE.
    // To allow for showing the HUD, mostly.
    public interface IHandsComponent : ISharedHandsComponent
    {
        IEntity GetEntity(string index);
        string ActiveIndex { get; }
        IEntity ActiveHand { get; }

        void SendChangeHand(string index);
        void AttackByInHand(string index);
        void UseActiveHand();
        void ActivateItemInHand(string handIndex);
        void RefreshInHands();
    }
}
