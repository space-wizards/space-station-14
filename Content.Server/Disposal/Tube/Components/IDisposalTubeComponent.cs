using Content.Server.Disposal.Unit.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.Disposal.Tube.Components
{
    public interface IDisposalTubeComponent : IComponent
    {
        Container Contents { get; }

        Direction NextDirection(DisposalHolderComponent holder);
        Vector2 ExitVector(DisposalHolderComponent holder);
        bool Remove(DisposalHolderComponent holder);
        bool TransferTo(DisposalHolderComponent holder, IDisposalTubeComponent to);
        bool CanConnect(Direction direction, IDisposalTubeComponent with);
        void PopupDirections(IEntity entity);
    }
}
