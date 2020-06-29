using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components.Disposal
{
    public interface IDisposalTubeComponent : IComponent
    {
        Container Contents { get; }
        Dictionary<Direction, IDisposalTubeComponent> Connected { get; }

        Direction NextDirection(InDisposalsComponent inDisposals);
        [CanBeNull] IDisposalTubeComponent NextTube(InDisposalsComponent inDisposals);
        bool Remove(InDisposalsComponent inDisposals);
        bool TransferTo(InDisposalsComponent inDisposals, IDisposalTubeComponent to);
        bool AdjacentConnected(Direction direction, IDisposalTubeComponent tube);
        void AdjacentDisconnected(IDisposalTubeComponent adjacent);
    }
}
