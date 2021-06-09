using System.Collections.Generic;
using Content.Server.GameObjects.Components.Construction;
using Robust.Shared.Analyzers;

namespace Content.Server.Interfaces.GameObjects
{
    [RequiresExplicitImplementation]
    public interface IRefreshParts
    {
        void RefreshParts(IEnumerable<MachinePartComponent> parts);
    }
}
