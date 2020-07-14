#nullable enable
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components.Disposal
{
    public interface IDisposalTubeComponent : IComponent
    {
        Container Contents { get; }

        Direction NextDirection(DisposableComponent disposable);
        Vector2 ExitVector(DisposableComponent disposable);
        IDisposalTubeComponent? NextTube(DisposableComponent disposable);
        bool Remove(DisposableComponent disposable);
        bool TransferTo(DisposableComponent disposable, IDisposalTubeComponent to);
        bool AdjacentConnected(Direction direction, IDisposalTubeComponent tube);
        void AdjacentDisconnected(IDisposalTubeComponent adjacent);
    }
}
