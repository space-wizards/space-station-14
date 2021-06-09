using System.Collections.Generic;
using Robust.Shared.Analyzers;

namespace Content.Server.Construction.Components
{
    [RequiresExplicitImplementation]
    public interface IRefreshParts
    {
        void RefreshParts(IEnumerable<MachinePartComponent> parts);
    }
}
