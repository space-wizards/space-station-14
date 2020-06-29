using System.Collections.Generic;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components.Disposal
{
    public interface IDisposalTubeComponent : IComponent
    {
        Container Contents { get; }
        Dictionary<Direction, IDisposalTubeComponent> Connected { get; }

        IDisposalTubeComponent NextTube(InDisposalsComponent inDisposals);
        bool Remove(InDisposalsComponent inDisposals);
        bool TransferTo(InDisposalsComponent inDisposals, IDisposalTubeComponent to);
        void AdjacentConnected(Direction direction, IDisposalTubeComponent tube);
    }
}
