using Content.Server.Disposal.Unit.Components;

namespace Content.Server.Disposal.Tube.Components
{
    public interface IDisposalTubeComponent : IComponent
    {
        /// <summary>
        ///     The directions that this tube can connect to others from
        /// </summary>
        /// <returns>a new array of the directions</returns>
        Direction[] ConnectableDirections();

        Direction NextDirection(DisposalHolderComponent holder);
    }
}
