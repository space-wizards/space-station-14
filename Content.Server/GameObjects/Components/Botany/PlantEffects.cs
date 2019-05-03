using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.Components.Botany
{
    /// <summary>
    /// Any nonheritable state of the plant goes here.
    /// </summary>
    class PlantEffects : IExposeData
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public string currentLifecycleNodeID = null;
        [ViewVariables(VVAccess.ReadWrite)]
        public double cellularAgeInSeconds = 0.0;
        [ViewVariables(VVAccess.ReadWrite)]
        public double lifeProgressInSeconds = 0.0;
        [ViewVariables(VVAccess.ReadWrite)]
        public double YieldMultiplier = 1.0;
        [ViewVariables(VVAccess.ReadWrite)]
        public bool dead;


        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref currentLifecycleNodeID, "currentLifecycleNodeID", null);
            serializer.DataField(ref cellularAgeInSeconds, "cellularAgeInSeconds", 0.0);
            serializer.DataField(ref lifeProgressInSeconds, "lifeProgressInSeconds", 0.0);
            serializer.DataField(ref YieldMultiplier, "yieldMultiplier", 1.0);
            serializer.DataField(ref dead, "dead", false);
        }
    }
}
