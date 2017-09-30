using SS14.Shared.Interfaces.GameObjects;

namespace Content.Client.Interfaces.GameObjects
{
    // HYPER SIMPLE HANDS API CLIENT SIDE.
    // To allow for showing the HUD, mostly.
    public interface IHandsComponent
    {
        IEntity GetEntity(string index);
        string ActiveIndex { get; }

        void SendChangeHand(string index);
    }
}
