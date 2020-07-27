#nullable enable
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components.Disposal
{
    public interface IDisposalTubeComponent : IComponent
    {
        Container Contents { get; }

        Direction NextDirection(DisposalHolderComponent holder);
        Vector2 ExitVector(DisposalHolderComponent holder);
        IDisposalTubeComponent? NextTube(DisposalHolderComponent holder);
        bool Remove(DisposalHolderComponent holder);
        bool TransferTo(DisposalHolderComponent holder, IDisposalTubeComponent to);
        bool AdjacentConnected(Direction direction, IDisposalTubeComponent tube);
        void AdjacentDisconnected(IDisposalTubeComponent adjacent);
        void MoveEvent(MoveEvent moveEvent);
        void PopupDirections(IEntity entity);
    }
}
