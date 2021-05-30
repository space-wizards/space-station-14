#nullable enable
using Robust.Shared.Players;

namespace Content.Shared.GameObjects.Components.Movement
{
    public interface IRelayMoveInput
    {
        void MoveInputPressed(ICommonSession session);
    }
}
