using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Disposal
{
    public interface IDisposalTubeComponent : IComponent
    {
        DisposalNet Parent { get; }
        bool Reconnecting { get; set; }
        Container Contents { get; }
        IEnumerable<IEntity> ContainedEntities { get; }
        void SpreadDisposalNet();
        bool CanConnectTo([NotNullWhen(true)] out DisposalNet parent);
        void ConnectToNet([JetBrains.Annotations.NotNull] DisposalNet net);
        void DisconnectFromNet();
        void Update(float frameTime, IEntity entity);
    }
}
