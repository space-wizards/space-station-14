using System.Collections.Generic;
using Content.Server.GameObjects.Components.Construction;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.Interfaces.GameObjects
{
    public interface IRefreshParts
    {
        void RefreshParts(IEnumerable<MachinePartComponent> parts);
    }
}
