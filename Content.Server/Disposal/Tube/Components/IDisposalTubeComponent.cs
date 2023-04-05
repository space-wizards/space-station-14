using Content.Server.Disposal.Unit.Components;
using Robust.Shared.Containers;

namespace Content.Server.Disposal.Tube.Components
{
    public interface IDisposalTubeComponent : IComponent
    {
        Container Contents { get; }

        Direction NextDirection(DisposalHolderComponent holder);
        bool CanConnect(Direction direction, IDisposalTubeComponent with);
        void PopupDirections(EntityUid entity);
    }
}
