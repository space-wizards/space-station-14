#nullable enable
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Robust.Shared.GameObjects;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.Power
{
    public interface IGridPowerComponent
    {
        void AddApcNet(IApcNet apcNet);

        void RemoveApcNet(IApcNet apcNet);

        void Update(float frameTime);
    }

    [ComponentReference(typeof(IGridPowerComponent))]
    [RegisterComponent] //[Serializable] - TODO: is this needed?
    public class GridPowerComponent : Component, IGridPowerComponent
    {
        public void AddApcNet(IApcNet apcNet)
        {
            ApcNets.Add(apcNet);
        }

        public void RemoveApcNet(IApcNet apcNet)
        {
            ApcNets.Remove(apcNet);
        }

        public override string Name => "GridPower";

        private HashSet<IApcNet> ApcNets { get; set; } = new();

        public void Update(float frameTime)
        {
            foreach (var apcNet in ApcNets)
            {
                apcNet.Update(frameTime);
            }
        }
    }
}
