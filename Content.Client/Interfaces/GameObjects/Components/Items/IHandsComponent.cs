using System.Collections.Generic;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Client.Interfaces.GameObjects.Components.Items
{
    // HYPER SIMPLE HANDS API CLIENT SIDE.
    // To allow for showing the HUD, mostly.
    public interface IHandsComponent
    {
        IReadOnlyDictionary<string, IEntity> Hands { get; }
        string ActiveIndex { get; }
        IEntity ActiveHand { get; }

        IEntity GetEntity(string index);
        void SendChangeHand(string index);
        void AttackByInHand(string index);
        void UseActiveHand();
        void ActivateItemInHand(string handIndex);
        void RefreshInHands();
    }
}
