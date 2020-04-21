using Robust.Server.Interfaces.Player;

namespace Content.Server.Interfaces.GameObjects.Components.Movement
{
    public interface IRelayMoveInput
    {
        void MoveInputPressed(IPlayerSession session);
    }
}
