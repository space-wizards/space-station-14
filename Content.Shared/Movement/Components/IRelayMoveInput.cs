using Robust.Shared.Players;

namespace Content.Shared.Movement.Components
{
    public interface IRelayMoveInput
    {
        void MoveInputPressed(ICommonSession session);
    }
}
