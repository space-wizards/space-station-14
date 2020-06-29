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
        void AdjacentConnected(Direction direction, IDisposalTubeComponent tube);
        void Update(float frameTime, IEntity entity);
    }
}
